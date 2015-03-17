using System.Collections.Generic;
using Nuget.PackageIndex.Models;
using Nuget.PackageIndex.Logging;

namespace Nuget.PackageIndex
{
    public class PackageSearcher : IPackageSearcher
    {
        private readonly IPackageIndexFactory _indexFactory;

        public PackageSearcher(ILog logger)
            : this(new PackageIndexFactory(logger))
        {
        }

        public PackageSearcher(IPackageIndexFactory indexFactory)
        {
            _indexFactory = indexFactory;
        }

        public IEnumerable<TypeModel> Search(string typeName)
        {
            var localIndex = _indexFactory.GetLocalIndex();
            if (localIndex == null)
            {
                return null;
            }

            var result = localIndex.GetTypes(typeName);
            if (result == null)
            {
                var remoteIndex = _indexFactory.GetRemoteIndex();
                if (remoteIndex != null)
                {
                    result = remoteIndex.GetTypes(typeName);
                }
            }

            return result;
        }
    }
}
