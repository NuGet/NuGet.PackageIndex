// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Nuget.PackageIndex.Client
{
    public class ProjectMetadata
    {
        public string ProjectPath { get; set; }
        public List<TargetFrameworkMetadata> TargetFrameworks { get; set; }
    }
}
