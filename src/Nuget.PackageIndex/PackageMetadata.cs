// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Collections.Generic;

namespace Nuget.PackageIndex
{
    /// <summary>
    /// Provides an abstracted representation of package to be used in public types
    /// </summary>
    internal class PackageMetadata : IPackageMetadata
    {
        public string Id { get; set; }
        public string Version{ get; set; }
        public IEnumerable<string> TargetFrameworks { get; set; }
        public string LocalPath { get; set; }
        public IEnumerable<AssemblyMetadata> Assemblies { get; set; }

        private ILocalPackageLoader PackageLoader { get; set; }

        public PackageMetadata(ILocalPackageLoader packageLoader)
        {
            PackageLoader = packageLoader;
        }

        public void Load()
        {
            PackageLoader.LoadPackage(this);
        }

        public bool Equals(IPackageMetadata other)
        {
            return Id.Equals(other.Id)
                   && Version.Equals(other.Version)
                   && LocalPath.Equals(other.LocalPath)
                   && AreTargetFrameworksEqual(TargetFrameworks, other.TargetFrameworks)
                   && AreAssembliesEqual(Assemblies, other.Assemblies);
        }

        private static bool AreTargetFrameworksEqual(IEnumerable<string> first, IEnumerable<string> other)
        {
            if (other == first)
            {
                return true;
            }

            if (other == null || first == null)
            {
                return false;
            }

            if (other.Count() != first.Count())
            {
                return false;
            }

            foreach (var fx in first)
            {
                if (!other.Any(x => x.Equals(fx, StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AreAssembliesEqual(IEnumerable<AssemblyMetadata> first, IEnumerable<AssemblyMetadata> other)
        {
            if (other == first)
            {
                return true;
            }

            if (other == null || first == null)
            {
                return false;
            }

            if (other.Count() != first.Count())
            {
                return false;
            }

            foreach (var fx in first)
            {
                if (!other.Any(x => x.FullPath.Equals(fx.FullPath, StringComparison.OrdinalIgnoreCase)
                                    && AreTargetFrameworksEqual(x.TargetFrameworks, fx.TargetFrameworks)))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
