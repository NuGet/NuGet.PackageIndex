// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;

namespace Nuget.PackageIndex.Models
{
    public class PackageInfo: ModelBase, IEquatable<PackageInfo>
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

        public override bool Equals(object obj)
        {
            PackageInfo other = obj as PackageInfo;
            if (other != null)
            {
                return Equals(other);
            }
            return false;
        }


        public bool Equals(PackageInfo other)
        {
            if (other == null)
            {
                return false;
            }

            return string.Compare(Name, other.Name, ignoreCase: true) == 0
                   && string.Compare(Version, other.Version, ignoreCase: true) == 0
                   && string.Compare(Path, other.Path, ignoreCase: true) == 0;
        }

        public override int GetHashCode()
        {
            return unchecked((string.IsNullOrEmpty(Name) ? 0 : Name.ToLowerInvariant().GetHashCode())
                + (string.IsNullOrEmpty(Version) ? 0 : Version.ToLowerInvariant().GetHashCode())
                + (string.IsNullOrEmpty(Path) ? 0 : Path.ToLowerInvariant().GetHashCode()));
        }

    }
}
