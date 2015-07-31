// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Text;

namespace Nuget.PackageIndex.Models
{
    /// <summary>
    /// Namespace metadata exposed publicly 
    /// </summary>
    public class NamespaceInfo : ModelBase, IPackageIndexModelInfo
    { 
        public string Name { get; set; }
        public string AssemblyName { get; set; }
        public string PackageName { get; set; }
        public string PackageVersion { get; set; }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(Name)
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
            return null; // it is already a namespace , so has no parent 
        }
    }
}
