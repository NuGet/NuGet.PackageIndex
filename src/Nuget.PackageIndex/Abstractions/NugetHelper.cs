// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System.IO;
using System.Collections.Generic;
using System.Runtime.Versioning;
using NuGet;

namespace Nuget.PackageIndex.Abstractions
{
    public class NugetHelper : INugetHelper
    {
        private ZipPackage _package;

        public void OpenPackage(Stream stream)
        {
            _package = new ZipPackage(stream);
        }

        public IEnumerable<IPackageFile> GetPackageFiles()
        {
            return _package.GetFiles();
        }

        public IEnumerable<FrameworkName> GetPackageSupportedFrameworks()
        {
            return _package.GetSupportedFrameworks();
        }

        public string GetPackageId()
        {
            return _package.Id;
        }

        public string GetPackageVersion()
        {
            return _package.Version.ToString();
        }
    }
}
