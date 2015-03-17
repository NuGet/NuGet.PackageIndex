using NuGet;
using System;

namespace Nuget.PackageIndex
{
    public class LocalPackageIndexBuilderResult
    {
        public bool Success { get; set; }
        public TimeSpan TimeElapsed { get; set; }
    }
}
