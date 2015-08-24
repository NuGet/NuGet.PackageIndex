// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Runtime.Versioning;
using Nuget.PackageIndex.NugetHelpers;

namespace Nuget.PackageIndex
{
    /// <summary>
    /// Discovers packages from nupkg files under given source paths. Needed
    /// to abstract out the way we discover packages in the future.
    /// </summary>
    internal class NupkgLocalPackageLoader : ILocalPackageLoader
    {
        private readonly Abstractions.IFileSystem _fileSystem;
        private readonly Abstractions.INugetHelper _nugetHelper;

        public NupkgLocalPackageLoader()
            : this(new Abstractions.FileSystem(), new Abstractions.NugetHelper())
        {
        }

        public NupkgLocalPackageLoader(Abstractions.IFileSystem fileSystem, Abstractions.INugetHelper nugetHelper)
        {
            _fileSystem = fileSystem;
            _nugetHelper = nugetHelper;
        }

        /// <summary>
        /// Discovers packages from nupkg files under given source paths
        /// </summary>
        public IEnumerable<IPackageMetadata> DiscoverPackages(IEnumerable<string> sourcePaths,
                                                              HashSet<string> indexedPackages,
                                                              bool newOnly,
                                                              DateTime lastIndexModifiedTime,
                                                              CancellationToken cancellationToken,
                                                              Func<string, bool> shouldIncludeFunc)
        {
            var nupkgFiles = GetPackages(sourcePaths, indexedPackages, newOnly, lastIndexModifiedTime, cancellationToken);
            foreach (var nupkgFile in nupkgFiles)
            {
                var newPackage = GetPackageMetadataFromPath(nupkgFile, shouldIncludeFunc);
                if (newPackage != null)
                {
                    yield return newPackage;
                }
            }
        }

        /// <summary>
        /// Returns metadata info for given nupkg file. 
        /// Note: Don't hold any object referenecs for ZipPackage data, since it might hold whole package in the memory.
        /// </summary>
        public IPackageMetadata GetPackageMetadataFromPath(string nupkgFilePath, Func<string, bool> shouldIncludeFunc)
        {
            if (string.IsNullOrEmpty(nupkgFilePath))
            {
                return null;
            }

            if (!_fileSystem.FileExists(nupkgFilePath))
            {
                return null;
            }

            // Note: don't use ZipPackage ctor that takes Stream, it stores package contents in memory and they 
            // are not collected after even though we are not referencing any of ZipPackage objects. Instead use 
            // ZipPackage(filePath) ctor.
            var package = _nugetHelper.OpenPackage(nupkgFilePath, (p) => { return new NuGet.ZipPackage(p); });

            // check if package id should be excluded and exit early
            if (shouldIncludeFunc != null && !shouldIncludeFunc(package.Id))
            {
                return null;
            }

            var packageFolder = Path.GetDirectoryName(nupkgFilePath) ?? string.Empty;

            // Note: If using this commented code to get package assemblies, it unpacks whole package into memory,
            // and then it is never garbage collected even though we don't keep any references to it. It is also 
            // should not be cached since ctor that we use sets _enableCaching = false for ZipPackage. So it might
            // have some unmanaged objects that are not collected by some reason.
            // var allAssemblies = package.GetFiles()
            //                            .Where(x => ".dll".Equals(Path.GetExtension(x.EffectivePath), StringComparison.OrdinalIgnoreCase))
            //                            .Select(x => x.Path.ToString())
            //                            .ToList();

            // we assume that nupkg file lives in the package dir and has lib, ref an dother package dirs in the same dir
            var allAssemblies = _fileSystem.DirectoryGetFiles(packageFolder, "*.dll", SearchOption.AllDirectories)
                                            .Select(x => x.Substring(packageFolder.Length + 1))
                                            .ToList();

            var packageTargetFrameworkNames = package.GetSupportedFrameworks().ToList() ?? Enumerable.Empty<FrameworkName>();
            var tfmAssemblyGroups = new Dictionary<FrameworkName, IList<string>>();

            // here we construct a list of the form { TFM }, { list of target assemblies } 
            foreach (var tfm in packageTargetFrameworkNames)
            {
                var dnxResult = TfmPackageAssemblyMatcher.GetAssembliesForFramework(tfm, package, allAssemblies);
                if (dnxResult != null)
                {
                    tfmAssemblyGroups.Add(new FrameworkName(tfm.Identifier, tfm.Version, tfm.Profile), dnxResult.ToList());
                }
            }

            // now we need to convert it to the list of the form: { assembly }, { list of TFM }
            var assembliesMetadata = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in tfmAssemblyGroups)
            {
                foreach (var assemblyPath in kvp.Value)
                {
                    List<string> existingAssemblyTfms = null;
                    if (assembliesMetadata.TryGetValue(assemblyPath, out existingAssemblyTfms))
                    {
                        existingAssemblyTfms.Add(DnxVersionUtility.GetShortFrameworkName(kvp.Key));
                    }
                    else
                    {
                        assembliesMetadata.Add(assemblyPath, new List<string> { DnxVersionUtility.GetShortFrameworkName(kvp.Key) });
                    }
                }
            }

            var selectedAssemblies = new List<AssemblyMetadata>();
            foreach (var am in assembliesMetadata)
            {
                var fullPath = Path.Combine(packageFolder, am.Key);

                if (!_fileSystem.FileExists(fullPath))
                {
                    fullPath = Path.Combine(packageFolder, package.Id.ToString(), am.Key);
                    if (!_fileSystem.FileExists(fullPath))
                    {
                        continue;
                    }
                }

                selectedAssemblies.Add(new AssemblyMetadata
                {
                    FullPath = fullPath,
                    TargetFrameworks = am.Value
                });
            }

            var newPackageMetadata = new PackageMetadata
            {
                Id = package.Id.ToString(),
                Version = package.Version.ToString(),
                LocalPath = nupkgFilePath,
                TargetFrameworks = packageTargetFrameworkNames.Select(x => DnxVersionUtility.GetShortFrameworkName(x)).ToList(),
                Assemblies = selectedAssemblies
            };

            return newPackageMetadata;
        }

        /// <summary>
        /// Getting packages from given local folders that contain nupkg files.
        /// </summary>
        internal IEnumerable<string> GetPackages(IEnumerable<string> sourcePaths,
                                                HashSet<string> indexedPackages,
                                                bool newOnly,
                                                DateTime lastIndexModifiedTime,
                                                CancellationToken cancellationToken)
        {
            Debug.Assert(sourcePaths != null);

            var packages = new List<string>();
            foreach (var source in sourcePaths)
            {
                if (!_fileSystem.DirectoryExists(source))
                {
                    continue;
                }

                var nupkgFiles = _fileSystem.DirectoryGetFiles(source, "*.nupkg", SearchOption.AllDirectories);
                foreach (var nupkgFile in nupkgFiles)
                {
                    if (cancellationToken != null && cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    if (newOnly)
                    {
                        if (_fileSystem.FileGetLastWriteTime(nupkgFile) <= lastIndexModifiedTime)
                        {
                            continue;
                        }
                    }
                    else if (indexedPackages != null && indexedPackages.Contains(nupkgFile))
                    {
                        continue;
                    }

                    yield return nupkgFile;
                }
            }
        }
    }
}
