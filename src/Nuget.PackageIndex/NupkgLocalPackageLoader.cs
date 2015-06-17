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
                                                              CancellationToken cancellationToken)
        {
            var result = new List<IPackageMetadata>();
            var nupkgFiles = GetPackages(sourcePaths, indexedPackages, newOnly, lastIndexModifiedTime, cancellationToken);
            foreach (var nupkgFile in nupkgFiles)
            {
                var newPackage = GetPackageMetadataFromPath(nupkgFile);
                if (newPackage != null)
                {
                    result.Add(newPackage);
                }
            }

            return result;
        }

        /// <summary>
        /// Returns metadata info for given nupkg file. 
        /// </summary>
        public IPackageMetadata GetPackageMetadataFromPath(string nupkgFilePath)
        {
            if (string.IsNullOrEmpty(nupkgFilePath))
            {
                return null;
            }

            if (!_fileSystem.FileExists(nupkgFilePath))
            {
                return null;
            }

            try
            {
                using (var fs = _fileSystem.FileOpenRead(nupkgFilePath))
                {
                    var package = _nugetHelper.OpenPackage(fs);

                    var packageFolder = Path.GetDirectoryName(nupkgFilePath) ?? string.Empty;
                    var allAssemblies = package.GetFiles()
                                               .Where(x => ".dll".Equals(Path.GetExtension(x.EffectivePath), StringComparison.OrdinalIgnoreCase))
                                               .Select(x => x.Path);

                    var packageTargetFrameworkNames = package.GetSupportedFrameworks() ?? new List<FrameworkName>();
                    var tfmAssemblyGroups = new Dictionary<FrameworkName, IEnumerable<string>>();
                    // here we construct a list of the form { TFM }, { list of target assemblies } 
                    foreach (var tfm in packageTargetFrameworkNames)
                    {
                        var dnxResult = TfmPackageAssemblyMatcher.GetAssembliesForFramework(tfm, package, allAssemblies);
                        if (dnxResult != null)
                        {
                            tfmAssemblyGroups.Add(tfm, dnxResult);
                        }
                    }

                    // now we need to convert it to the list of the form: { assembly }, { list of TFM }
                    var assembliesMetadata = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                    foreach(var kvp in tfmAssemblyGroups)
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
                            fullPath = Path.Combine(packageFolder, package.Id, am.Key);
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
                        Id = package.Id,
                        Version = package.Version.ToString(),
                        LocalPath = nupkgFilePath,
                        TargetFrameworks = packageTargetFrameworkNames.Select(x => DnxVersionUtility.GetShortFrameworkName(x)),
                        Assemblies = selectedAssemblies
                    };

                    return newPackageMetadata;
                }
            }
            catch (Exception e)
            {
                Debug.Write(e.ToString());
            }

            return null;
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
                try
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
                            return null;
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

                        packages.Add(nupkgFile);
                    }
                }
                catch (Exception e)
                {
                    Debug.Write(e.ToString());
                }
            }

            return packages;
        }
    }
}
