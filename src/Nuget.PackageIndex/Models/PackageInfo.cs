// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System.Text;

namespace Nuget.PackageIndex.Models
{
    public class PackageInfo: ModelBase
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string Path { get; set; }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(Name)
                         .Append(Version)
                         .Append(Path);

            return stringBuilder.ToString();
        }
    }
}
