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
                        var allAssemblies = package.GetFiles().Where(x => ".dll".Equals(Path.GetExtension(x.EffectivePath), StringComparison.OrdinalIgnoreCase)); 

                        // There is a new Nuget package format comming and we need to have slightly different logic 
                        // for package discovery depending on the nuget package format version:
                        //      
                        // Old nuget format:
                        //      - assemblies are located under \lib folder which can contain subfolders corresponding to
                        //        each target framework and containing assemblies to be referenced when package is installed
                        //      - \lib folder might have a "contract" folder under it, which would mean that all types are the
                        //        same accross all assemblies in all target frameworks and are contained in the \contract\assembly
                        //        (when assemblies under lib\fx\would have no types and use contract assemblies when they are referenced)
                        //
                        //      Logic for old nuget format:
                        //       - if there is a lib\contract assembly - use only that assembly and ignore lib\fx subfolders
                        //       - if there no lib\contract try to load types from all assemblies  in lib\fx folders
                        //      Note: just in case we do check all dlls now to make sure there no lib\fx specific types in some
                        //      assembies which are specific to that fx comparing to common types in a contract assembly.
                        //      So dlls under contract would have all package's target frameworks, dlls under fx folder would have 
                        //      only that particular target fx.
                        //
                        // New nuget format:
                        //      - assemblies live under lib\fx folders as before
                        //      - instdead of contract folder there is now Ref folder which contains common contracts. Unlice old contract
                        //        folder, Ref folder might contain subfolder Any meaning that those contracts belong to any target fx; and
                        //        corresponding Fx subfolder, which would contain types specific to particular FX.
                        // 
                        //      Logic for new format:
                        //       - if there is a ref folder use assemblies under ref\any and then ref\fx; ignore lib assemblies
                        //       - if there no ref folder just use assemblies from lib\fx subfolders
                        //  
                        // Note: just in case we process all assemblies lib, ref and contract. Assemblies with ref/contract that are in 
                        // lib folder would not contain any types normally, so they shoudl be quick to process.

                        var packageTargetFrameworks = package.GetSupportedFrameworks().Select(x => VersionUtility.GetShortFrameworkName(x));
                        var assembliesMetadata = new List<AssemblyMetadata>();
                        foreach (var assembly in allAssemblies)
                        {
                            var assemblyFullPath = Path.Combine(packageFolder, assembly.Path);
                            if (!File.Exists(assemblyFullPath))
                            {
                                assemblyFullPath = Path.Combine(packageFolder, Path.GetFileNameWithoutExtension(nupkgFilePath), assembly.Path);
                            }

                            if (!File.Exists(assemblyFullPath))
                            {
                                continue;
                            }

                            IEnumerable<string> assemblyTargetFrameworks =  assembly.SupportedFrameworks.Select(x => VersionUtility.GetShortFrameworkName(x));
                            if (!assemblyTargetFrameworks.Any())
                            {
                                assemblyTargetFrameworks = packageTargetFrameworks;
                            }

                            assembliesMetadata.Add(
                                new AssemblyMetadata
                                {
                                    FullPath = assemblyFullPath,
                                    TargetFrameworks = assemblyTargetFrameworks
                                }
                            );
                        }

                        var newPackageMetadata = new PackageMetadata
                        {
                            Id = package.Id,
                            Version = package.Version.ToString(),
                            LocalPath = nupkgFilePath,
                            TargetFrameworks = packageTargetFrameworks,
                            Assemblies = assembliesMetadata
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
