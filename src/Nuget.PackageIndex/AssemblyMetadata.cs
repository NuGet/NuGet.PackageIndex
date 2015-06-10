﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System.Collections.Generic;

namespace Nuget.PackageIndex
{
    public class AssemblyMetadata
    {
        public AssemblyMetadata()
        {
            TargetFrameworks = new List<string>();
        }
        public string FullPath { get; set; }
        public IEnumerable<string> TargetFrameworks { get; set; }
    }
}
