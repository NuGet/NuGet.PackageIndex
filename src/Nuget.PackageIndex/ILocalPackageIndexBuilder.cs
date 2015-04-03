using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nuget.PackageIndex
{
    /// <summary>
    /// Provides API for local index manipulation. Knows how to find packages on local machine.
    /// </summary>
    public interface ILocalPackageIndexBuilder
    {
        ILocalPackageIndex Index { get; }
        IEnumerable<string> GetPackages(bool newOnly);
        Task<LocalPackageIndexBuilderResult> BuildAsync(bool newOnly = false);
        LocalPackageIndexBuilderResult Clean();
        LocalPackageIndexBuilderResult Rebuild();
        LocalPackageIndexBuilderResult AddPackage(string nupkgFilePath, bool force = false);
        LocalPackageIndexBuilderResult RemovePackage(string packageName, bool force = false);
    }
}
