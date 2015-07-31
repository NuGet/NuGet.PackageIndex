// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Nuget.PackageIndex.Client
{
    /// <summary>
    /// Represents factory that can create a language specific IAddPackageAnalyzer
    /// </summary>
    public interface IAddPackageAnalyzerFactory
    {
        IAddPackageAnalyzer GetAnalyzer(string filePath);
    }
}
