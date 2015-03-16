using System.Collections.Generic;
using Nuget.PackageIndex.Models;

namespace Nuget.PackageIndex
{
    public class PackageSearcher : IPackageSearcher
    {
        public IEnumerable<TypeModel> Search(string typeName)
        {
            var index = new LocalPackageIndex();
            return index.GetTypes(typeName);
        }
    }
}
