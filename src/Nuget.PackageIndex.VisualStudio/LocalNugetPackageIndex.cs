// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Nuget.PackageIndex.VisualStudio
{
    /// <summary>
    /// Should be exported by project systems that update local nuget packages,
    /// to refresh local package index
    /// </summary>
    public sealed class LocalNugetPackageIndex : ILocalNugetPackageIndex
    {
        public const string RoslynHandshakeContract = "RoslynNuGetSuggestions";
        private static LocalNugetPackageIndex _instance;
        public static LocalNugetPackageIndex Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new LocalNugetPackageIndex();
                }

                return _instance;
            }
        }

        private const string IndexIDEVersionFile = "ide.txt";

        private static  Task _indexBuildTask;
        private IPackageIndexFactory _indexFactory;
        private ILocalPackageIndexBuilder _indexBuilder;

        private LocalNugetPackageIndex()
        {
            _indexFactory = new PackageIndexFactory();
            _indexBuilder = _indexFactory.GetLocalIndexBuilder(createIfNotExists: false);
        }

        #region ILocalNugetPackageIndex

        public void Initialize()
        {
            if (PackageIndexActivityLevelProvider.ActivityLevel > ActivityLevel.On)
            {
                return;
            }

            var shouldClean = false;

            //// if at least one directory where packages live does not exist, 
            //// we need to clean index and rebuild it from scratch. It would be 
            //// much faster than go package by package.
            //var packageDirs = _indexBuilder.GetPackageDirectories();
            //foreach(var packageDir in packageDirs)
            //{
            //    if (!Directory.Exists(packageDir))
            //    {
            //        shouldClean = true;
            //        try
            //        {
            //            // create directory to avoid cleaning at next VS startup
            //            Directory.CreateDirectory(packageDir);
            //        }
            //        catch(Exception e)
            //        {
            //            Debug.Write(e.ToString());
            //        }
            //    }
            //}

            // In addition to package sources we should also check WTE version,
            // since when there is new WTE installed, we have new set of default 
            // packages and need to clean index complettely and rebuild
            // Note: yes we tied to WTE now, but whole current version of the 
            // Package Index already very tied to WTE because of Roslyn limitation
            // and need to be redesigned to support all C# projects again after
            // Roslyn provides project info for file being analyzed and supports
            // VS UI thread correctly.
            var ideVersion = GetCurrentIDEVersion();
            var indexIDEVersion = GetIndexIDEVersion();
            if (!string.IsNullOrEmpty(ideVersion))
            {
                if (!ideVersion.Equals(indexIDEVersion, StringComparison.OrdinalIgnoreCase))
                {
                    // remember new IDE version and clean/rebuild index
                    shouldClean = true;
                    SetIndexIDEVersion(ideVersion);
                }
            }

            if (shouldClean)
            {
                _indexBuilder.Clean();
            }

            Synchronize(shouldClean: shouldClean, force: true);
        }

        public void Synchronize()
        {
            if (PackageIndexActivityLevelProvider.ActivityLevel > ActivityLevel.On)
            {
                return;
            }

            Synchronize(shouldClean: false, force: false);
        }

        public void Detach()
        {
            _indexFactory.DetachFromLocalIndex();
        }

        #endregion

        private void Synchronize(bool shouldClean, bool force)
        {
            try
            {
                if (_indexBuildTask == null || _indexBuildTask.IsCompleted)
                {
                    RebuildIndex(shouldClean, force);
                }
                else
                {
                    // if there is index build task already running, remember only last 
                    // synchronize request and continue after current rebuild is complete.
                    // This is to avoid resync 10 times when there multiple synchronize 
                    // requests come and we don't need actually to rebuild index for them 
                    // all, but just for the first and maybe the last one
                    _indexBuildTask.ContinueWith(t => RebuildIndex(shouldClean, force));
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
            }
        }

        private void RebuildIndex(bool shouldClean, bool force)
        {
            // Fire and forget. When new package is installed, we are not just adding this package to the index,
            // but instead we attempt full sync. Full sync in this case would only sync new packages that don't 
            // exist in the index which should be fast. The reason for full sync is that when there are several 
            // instances of VS and each tries to update index at the same tiime, only one would succeed, other 
            // would notive that index is locked and skip this operation. Thus if all VS instances attempt full 
            // sync at least one of them would do it and add all new packages to the index.
            _indexBuildTask = _indexBuilder.BuildAsync(shouldClean:shouldClean, newOnly: _indexBuilder.Index.IndexExists && !force, 
                                                       cancellationToken: _indexFactory.GetCancellationToken());
        }

        private string GetCurrentIDEVersion()
        {
            var version = string.Empty;

            var keys = new string[]
            {
                @"SOFTWARE\Wow6432Node\Microsoft\Web Tools\Visual Studio 14",
                @"SOFTWARE\Microsoft\Web Tools\Visual Studio 14",
            };

            foreach (var key in keys)
            {
                try
                {
                    using (RegistryKey versionKey = Registry.LocalMachine.OpenSubKey(key))
                    {
                        if (versionKey != null)
                        {
                            version = versionKey.GetValue("Version") as string;

                            if (!string.IsNullOrEmpty(version))
                            {
                                break;
                            }
                        }
                    }
                }
                catch(Exception e)
                {
                    Debug.Write(e.ToString());
                }
            }

            return version;
        }

        private string GetIndexIDEVersion()
        {
            var version = string.Empty;

            try
            {
                var indexIdeVarsionFilePath = Path.Combine(_indexBuilder.Index.Location, IndexIDEVersionFile);
                if (File.Exists(indexIdeVarsionFilePath))
                {
                    version = File.ReadAllText(indexIdeVarsionFilePath);
                }
            }
            catch(Exception e)
            {
                Debug.Write(e.ToString());
            }

            return version;
        }

        private void SetIndexIDEVersion(string ideVersion)
        {
            try
            {
                var indexIdeVarsionFilePath = Path.Combine(_indexBuilder.Index.Location, IndexIDEVersionFile);
                File.WriteAllText(indexIdeVarsionFilePath, ideVersion);
            }
            catch(Exception e)
            {
                Debug.Write(e.ToString());
            }
        }
    }
}
