// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using NuGet;
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
        /// Returns metadata info for given nupkg file
        /// </summary>
        /// <param name="nupkgFilePath"></param>
        /// <returns></returns>
        public IPackageMetadata GetPackageMetadataFromPath(string nupkgFilePath)
        {
            if (string.IsNullOrEmpty(nupkgFilePath))
            {
                return null;
            }

            if (!File.Exists(nupkgFilePath))
            {
                return null;
            }

            try
            {
                using (var fs = File.OpenRead(nupkgFilePath))
                {
                    var package = new ZipPackage(fs);
                    var packageFolder = Path.GetDirectoryName(nupkgFilePath) ?? "";
                    var assemblies = package.GetLibFiles().Where(x => ".dll".Equals(Path.GetExtension(x.EffectivePath), StringComparison.OrdinalIgnoreCase));

                    var assemblyPaths = new List<string>();
                    foreach (var assembly in assemblies)
                    {
                        var assemblyFullPath = Path.Combine(packageFolder, assembly.Path);
                        if (!File.Exists(assemblyFullPath))
                        {
                            assemblyFullPath = Path.Combine(packageFolder, Path.GetFileNameWithoutExtension(nupkgFilePath), assembly.Path);
                        }

                        if (File.Exists(assemblyFullPath))
                        {
                            assemblyPaths.Add(assemblyFullPath);
                        }
                    }

                    var newPackageMetadata = new PackageMetadata
                    {
                        Id = package.Id,
                        Version = package.Version.ToString(),
                        LocalPath = nupkgFilePath,
                        TargetFrameworks = package.GetSupportedFrameworks().Select(x => VersionUtility.GetShortFrameworkName(x)),
                        Assemblies = assemblyPaths
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
        private IEnumerable<string> GetPackages(IEnumerable<string> sourcePaths, 
                                                HashSet<string> indexedPackages, 
                                                bool newOnly,
                                                DateTime lastIndexModifiedTime, 
                                                CancellationToken cancellationToken)
        {
            var packages = new List<string>();
            foreach (var source in sourcePaths)
            {
                try
                {
                    if (!Directory.Exists(source))
                    {
                        continue;
                    }

                    var nupkgFiles = Directory.GetFiles(source, "*.nupkg", SearchOption.AllDirectories);
                    foreach (var nupkgFile in nupkgFiles)
                    {
                        if (cancellationToken != null && cancellationToken.IsCancellationRequested)
                        {
                            return null;
                        }

                        if (newOnly)
                        {
                            if (File.GetLastWriteTime(nupkgFile) <= lastIndexModifiedTime)
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
