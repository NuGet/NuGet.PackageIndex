using System.Collections.Generic;

namespace Nuget.PackageIndex.VisualStudio.Analyzers
{
    /// <summary>
    /// Stored for each file to avoid looking for DTE objects everytime
    /// </summary>
    public class FrameworkCacheItem
    {
        public string ProjectUniqueName { get; set; }
        public IEnumerable<string> TargetFrameworks { get; set; }
    }
}
