﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Nuget.PackageIndex.NugetHelpers
{
    public class ContentItem
    {
        public string Path { get; set; }
        public IDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

        public override string ToString()
        {
            return Path;
        }
    }
}
