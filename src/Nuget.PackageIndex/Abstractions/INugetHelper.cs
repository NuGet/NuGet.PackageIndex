// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System.IO;
using NuGet;

namespace Nuget.PackageIndex.Abstractions
{
    public interface INugetHelper
    {
        IPackage OpenPackage(Stream stream);
    }
}
