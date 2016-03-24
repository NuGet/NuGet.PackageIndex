// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using NuGet.Repositories;
using System.Collections.Generic;
using System.IO;

namespace Nuget.PackageIndex.Abstractions
{
    public interface INugetHelper
    {
        IEnumerable<string> GetPackageFiles(LocalPackageInfo package);
        Stream GetStream(LocalPackageInfo package, string path);
    }
}
