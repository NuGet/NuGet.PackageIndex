// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;

namespace Nuget.PackageIndex.Models
{
    /// <summary>
    /// Extension metadata exposed publicly 
    /// </summary>
    public class ExtensionInfo : ModelBase, IPackageIndexModelInfo
    { 
        public string Name { get; set; }
        public string FullName { get; set; }
        public string Namespace { get; set; }
        public string AssemblyName { get; set; }
        public string PackageName { get; set; }
        public string PackageVersion { get; set; }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(Namespace)
                         .Append(",")
                         .Append(FullName)
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
            return FullName;
        }

        public string GetFriendlyPackageName()
        {
            return string.Format("{0} {1}", PackageName, PackageVersion);
        }

        public string GetNamespace()
        {
            return Namespace;
        }
    }
}
