// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System.IO;
using System.Collections.Generic;
using System.Runtime.Versioning;
using NuGet;

namespace Nuget.PackageIndex.Abstractions
{
    public interface INugetHelper
    {
        void OpenPackage(Stream stream);
        IEnumerable<IPackageFile> GetPackageFiles();
        IEnumerable<FrameworkName> GetPackageSupportedFrameworks();
        string GetPackageId();
        string GetPackageVersion();
    }
}
