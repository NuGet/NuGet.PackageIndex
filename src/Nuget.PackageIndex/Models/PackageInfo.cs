// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System.Text;

namespace Nuget.PackageIndex.Models
{
    public class PackageInfo
    {
        public string Name { get; set; }
        public string Version { get; set; }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(Name)
                         .Append(Version);

            return stringBuilder.ToString();
        }
    }
}
