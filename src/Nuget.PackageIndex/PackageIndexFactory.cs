using System.Threading.Tasks;
using Nuget.PackageIndex.Logging;

namespace Nuget.PackageIndex
{
    /// <summary>
    /// Is responsible for initialization of the indexes. If there no local index,
    /// it would schedule a task that will create an index on user machine.
    /// </summary>
    internal class PackageIndexFactory : IPackageIndexFactory
    {
        private ILog _logger;
        private ILocalPackageIndex _index;

        public PackageIndexFactory(ILog logger)
        {
            _logger = logger;            
        }

        public ILocalPackageIndex GetLocalIndex()
        {
            if (_index == null)
            {
                _index = new LocalPackageIndex(_logger);

                if (!_index.IndexExists)
                {
                    Task.Run(() =>
                    {
                        // Fire and forget. While index is building, it will be locked from
                        // other write attempts. In meanwhile readers would just not be able 
                        // to find any types, but will be still operatable (when an instance of 
                        // a reader is created it can return data from the snapshot before next
                        // write happened).
                        var builder = new LocalPackageIndexBuilder(_index, _logger);
                        builder.Build();
                    });                    
                }
            }

            return _index;
        }

        public IRemotePackageIndex GetRemoteIndex()
        {
            return null; // remote index is not implemented yet
        }
    }
}
