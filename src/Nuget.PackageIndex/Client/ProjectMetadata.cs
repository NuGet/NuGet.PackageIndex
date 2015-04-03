using System.Collections.Generic;

namespace Nuget.PackageIndex.Client
{
    /// <summary>
    /// Code imported form Microsoft.CodeAnalysis since it is internal there and we need it
    /// </summary>
    public class TargetFrameworkMetadata
    {
        public string TargetFrameworkShortName { get; set; }
        public Dictionary<string, string> Packages { get; set; }
    }
}
