// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System.Collections.Generic;

namespace Nuget.PackageIndex.VisualStudio.Analyzers
{
    /// <summary>
    /// Stored for each file to avoid looking for DTE objects everytime
    /// </summary>
    public class FrameworkCacheItem
    {
        public string ProjectUniqueName { get; set; }
        public IEnumerable<string> TargetFrameworks { get; set; }
    }
}
