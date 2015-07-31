// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text;

namespace Nuget.PackageIndex.Models
{
    /// <summary>
    /// Type metadata exposed publicly 
    /// </summary>
    public class TypeInfo : ModelBase, IPackageIndexModelInfo
    { 
        public string Name { get; set; }
        public string FullName { get; set; }
        public string AssemblyName { get; set; }
        public string PackageName { get; set; }
        public string PackageVersion { get; set; }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(FullName)
                         .Append(",")
                         .Append(AssemblyName)
                         .Append(",")
                         .Append(PackageName)
                         .Append(" ")
                         .Append(PackageVersion)
                         .Append(", Target Frameworks: ")
                         .Append(GetTargetFrameworksString());

            return stringBuilder.ToString();
        }

        protected string GetTargetFrameworksString()
        {
            return string.Join(";", TargetFrameworks) ?? "";
        }

        public string GetFriendlyEntityName()
        {
            return Name;
        }

        public string GetFriendlyPackageName()
        {
            return string.Format("{0} {1}", PackageName, PackageVersion);
        }

        public string GetNamespace()
        {
            return FullName.Contains(".") ? Path.GetFileNameWithoutExtension(FullName) : null;
        }
    }
}
