// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Nuget.PackageIndex.Client
{
    public class TargetFrameworkMetadata
    {
        public string TargetFrameworkShortName { get; set; }
        public IEnumerable<string> Imports { get; set; }
        public Dictionary<string, string> Packages { get; set; }
    }
}
