using System.Collections.Generic;
using Nuget.PackageIndex.Models;

namespace Nuget.PackageIndex
{
    /// <summary>
    /// Wraps a search logic for given type instead of just using an instance of IPackageIndex.
    /// This is needed since there could be multiple versions of indexes and we need one place 
    /// for the logic that knows which version to use.
    /// </summary>
    public interface IPackageSearcher
    {
        IEnumerable<TypeInfo> Search(string typeName);
    }
}
