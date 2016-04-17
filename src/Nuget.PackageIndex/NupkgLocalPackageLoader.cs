// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using NuGet.Client;
using NuGet.Frameworks;
using NuGet.Repositories;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

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
        public IPackageMetadata GetPackageMetadataFromPath(string nuspecFilePath, Func<string, bool> shouldIncludeFunc)
        {
            var package = GetPackageInfo(nuspecFilePath);
            if (package == null)
            {
                return null;
            }

            // check if package id should be excluded and exit early
            if (shouldIncludeFunc != null && !shouldIncludeFunc(package.Id))
            {
                return null;
            }

            var newPackageMetadata = new PackageMetadata(this)
            {
                Id = package.Id.ToString(),
                Version = package.Version.ToString(),
                LocalPath = package.ManifestPath
            };

            return newPackageMetadata;
        }

        public void LoadPackage(IPackageMetadata packageMetadata)
        {
            var package = GetPackageInfo(packageMetadata.LocalPath);
            if (package == null)
            {
                return;
            }

            var allAssemblies = _nugetHelper.GetPackageFiles(package)
                                             .Where(x => !string.IsNullOrEmpty(x) && x.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                                             .ToList();


            var packageTargetFrameworkNames = package.Nuspec.GetDependencyGroups()
                                                            .Select(x => x.TargetFramework)
                                                            .Distinct()
                                                            .ToList();
            // just in case take also framework names from assemblies' folder names
            foreach (var assemblyPath in allAssemblies)
            {
                if (!assemblyPath.StartsWith("lib/") && !assemblyPath.StartsWith("ref/"))
                {
                    continue;
                }

                var indexOfThSeparator = assemblyPath.IndexOf('/', 4);
                var fxFolder = assemblyPath.Substring(4, indexOfThSeparator - 4);

                var nugetFx = NuGetFramework.Parse(fxFolder);
                if (nugetFx != null && !packageTargetFrameworkNames.Any(x => x.Equals(nugetFx)))
                {
                    packageTargetFrameworkNames.Add(nugetFx);
                }
            }

            var tfmAssemblyGroups = new Dictionary<NuGetFramework, IList<string>>();

            // here we construct a list of the form { TFM }, { list of target assemblies }
            foreach (var tfm in packageTargetFrameworkNames)
            {
                var dnxResult = GetAssembliesForFramework(package, tfm, allAssemblies);
                if (dnxResult != null)
                {
                    tfmAssemblyGroups.Add(tfm, dnxResult.ToList());
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
                        existingAssemblyTfms.Add(kvp.Key.GetShortFolderName());
                    }
                    else
                    {
                        assembliesMetadata.Add(assemblyPath, new List<string> { kvp.Key.GetShortFolderName() });
                    }
                }
            }

            var selectedAssemblies = assembliesMetadata.Where(x => x.Value.Any())
                                                       .Select(x => new AssemblyMetadata
                                                       {
                                                           FullPath = x.Key,
                                                           TargetFrameworks = x.Value,
                                                           Package = package
                                                       })
                                                       .ToList();

            packageMetadata.TargetFrameworks = packageTargetFrameworkNames.Select(x => x.GetShortFolderName()).ToList();
            packageMetadata.Assemblies = selectedAssemblies;
        }

        private LocalPackageInfo GetPackageInfo(string nuspecFilePath)
        {
            if (string.IsNullOrEmpty(nuspecFilePath))
            {
                return null;
            }

            if (!_fileSystem.FileExists(nuspecFilePath))
            {
                return null;
            }

            var id = Path.GetFileNameWithoutExtension(nuspecFilePath);
            var fullVersionDir = Path.GetDirectoryName(nuspecFilePath);
            var versionString = Path.GetFileName(fullVersionDir);
            if (versionString.Equals(id))
            {
                // old format of local package folders, we don't support it now, since WTE ships feed
                // in new format already.
                return null;
            }

            NuGetVersion version;
            if (!NuGetVersion.TryParse(versionString, out version))
            {
                return null;
            }

            var nupkgFilePath = _fileSystem.DirectoryGetFiles(fullVersionDir, "*.nupkg", SearchOption.TopDirectoryOnly)
                                           .FirstOrDefault();
            return nupkgFilePath == null
                ? null
                : new LocalPackageInfo(id, version, fullVersionDir, nuspecFilePath, nupkgFilePath);
        }

        private IEnumerable<string> GetAssembliesForFramework(LocalPackageInfo package, NuGetFramework framework, IEnumerable<string> files)
        {
            var contentItems = new NuGet.ContentModel.ContentItemCollection();
            HashSet<string> referenceFilter = null;

            contentItems.Load(files);

            // This will throw an appropriate error if the nuspec is missing
            var nuspec = package.Nuspec;
            IList<string> compileTimeAssemblies = null;
            IList<string> runtimeAssemblies = null;

            var referenceSet = nuspec.GetReferenceGroups().GetNearest(framework);
            if (referenceSet != null)
            {
                referenceFilter = new HashSet<string>(referenceSet.Items, StringComparer.OrdinalIgnoreCase);
            }

            var conventions = new ManagedCodeConventions(null);            
            var managedCriteria = conventions.Criteria.ForFramework(framework);
            var compileGroup = contentItems.FindBestItemGroup(managedCriteria, conventions.Patterns.CompileAssemblies, conventions.Patterns.RuntimeAssemblies);

            if (compileGroup != null)
            {
                compileTimeAssemblies = compileGroup.Items.Select(t => t.Path).ToList();
            }

            var runtimeGroup = contentItems.FindBestItemGroup(managedCriteria, conventions.Patterns.RuntimeAssemblies);
            if (runtimeGroup != null)
            {
                runtimeAssemblies = runtimeGroup.Items.Select(p => p.Path).ToList();
            }

            // COMPAT: Support lib/contract so older packages can be consumed
            var contractPath = "lib/contract/" + package.Id + ".dll";
            var hasContract = files.Any(path => path == contractPath);
            var hasLib = runtimeAssemblies?.Any();

            if (hasContract
                && hasLib.HasValue
                && hasLib.Value
                && !framework.IsDesktop())
            {
                compileTimeAssemblies.Clear();
                compileTimeAssemblies.Add(contractPath);
            }

            // Apply filters from the <references> node in the nuspec
            if (referenceFilter != null)
            {
                // Remove anything that starts with "lib/" and is NOT specified in the reference filter.
                // runtimes/* is unaffected (it doesn't start with lib/)
                compileTimeAssemblies = compileTimeAssemblies.Where(p => !p.StartsWith("lib/") || referenceFilter.Contains(Path.GetFileName(p))).ToList();
            }

            return compileTimeAssemblies ?? Enumerable.Empty<string>();
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

                var nupkgFiles = _fileSystem.DirectoryGetFilesUpTo2Deep(source, "*.nuspec");
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
