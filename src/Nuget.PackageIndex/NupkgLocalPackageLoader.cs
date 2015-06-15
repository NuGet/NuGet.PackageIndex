// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using NuGet;

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
        /// Returns metadata info for given nupkg file. The logic for picking right
        /// reference asemblies is:
        /// 
        /// Action Path                                     Target
        /// ====== ======================================== ======
        /// +      lib\assembly.dll                         any
        /// +      lib\fx\asembly.dll                       fx
        /// -      tools\assembly.dll                       -
        /// -      content\assembly.dll                     -
        /// +      lib\contract\assembly.dll                any
        /// +      ref\any\assembly.dll                     any
        /// +      ref\fx\asembly.dll                       fx
        /// 
        /// If ref folder exists - take it,
        /// otherwise if contract folder exist - take it,
        /// otherwise process assemlies under lib.
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
                    _nugetHelper.OpenPackage(fs);

                    var packageFolder = Path.GetDirectoryName(nupkgFilePath) ?? string.Empty;
                    var allAssemblies = _nugetHelper.GetPackageFiles()
                                                    .Where(x => ".dll".Equals(Path.GetExtension(x.EffectivePath), StringComparison.OrdinalIgnoreCase)
                                                                && !x.Path.StartsWith(@"tools\", StringComparison.OrdinalIgnoreCase)
                                                                && !x.Path.StartsWith(@"content\", StringComparison.OrdinalIgnoreCase));

                    var hasContractAssemblies = false;
                    var refAssemblies = allAssemblies.Where(x => x.Path.StartsWith(@"ref\", StringComparison.OrdinalIgnoreCase));
                    if (refAssemblies.Any())
                    {
                        // if it is a new format, take only ref assembies  
                        allAssemblies = refAssemblies;
                    }
                    else
                    {
                        // if it is old format and it has a "contract" subfolder, take only assemblies under contract
                        var contractAssemblies = allAssemblies.Where(x => x.Path.StartsWith(@"lib\contract\", StringComparison.OrdinalIgnoreCase));
                        if (contractAssemblies.Any())
                        {
                            allAssemblies = contractAssemblies;
                            hasContractAssemblies = true;
                        }
                    }

                    // otherwise it is an old format without contract folder, i.e. having only
                    // fx folders under lib or assemblies directly under lib root
                    var packageTargetFrameworks = _nugetHelper.GetPackageSupportedFrameworks()
                                                              .Select(x => VersionUtility.GetShortFrameworkName(x));
                    var assembliesMetadata = new List<AssemblyMetadata>();
                    foreach (var assembly in allAssemblies)
                    {
                        // packages under %programFiles%\WebTools\DNU and .dnx are located in the same folder as nupkg file
                        var assemblyFullPath = Path.Combine(packageFolder, assembly.Path);
                        if (!_fileSystem.FileExists(assemblyFullPath))
                        {
                            // packages under %ProgramFiles%\WebTools\packages are located in PackaeName subfolder
                            assemblyFullPath = Path.Combine(packageFolder, 
                                                            Path.GetFileNameWithoutExtension(nupkgFilePath), assembly.Path);

                            if (!_fileSystem.FileExists(assemblyFullPath))
                            {
                                continue;
                            }
                        }

                        IEnumerable<string> assemblyTargetFrameworks = assembly.SupportedFrameworks
                                                                               .Select(x => VersionUtility.GetShortFrameworkName(x));
                        // determine assembly framework folder name (it could be also lib or any)
                        var potentialFrameworkShortName = Path.GetFileName(Path.GetDirectoryName(assembly.Path) ?? string.Empty);
                        var isUnderLibRoot = "lib".Equals(potentialFrameworkShortName, StringComparison.OrdinalIgnoreCase);
                        if (isUnderLibRoot 
                            || potentialFrameworkShortName.Equals("any", StringComparison.OrdinalIgnoreCase)
                            || hasContractAssemblies)
                        {
                            // if it is directly under lib root or under ref\any or under lib\contract,
                            // then it target frameworks should be all frameworks supported by package
                            assemblyTargetFrameworks = packageTargetFrameworks;
                        }
                        else
                        {
                            // otherwise, if supported frameworks did not return anything explicitly,
                            // then use fx folder name as assembly target framework short name
                            if (!assemblyTargetFrameworks.Any())
                            {
                                assemblyTargetFrameworks = new[] { potentialFrameworkShortName };
                            }
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
                        Id = _nugetHelper.GetPackageId(),
                        Version = _nugetHelper.GetPackageVersion(),
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
