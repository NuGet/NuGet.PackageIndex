using Lucene.Net.Documents;
using Lucene.Net.Search;

namespace Nuget.PackageIndex.Models
{
    /// <summary>
    /// Package Index model representation, all models must implement it if they want to be 
    /// stored in the index.
    /// </summary>
    public interface IPackageIndexModel
    {
        Document ToDocument();
        Query GetDefaultSearchQuery();
    }
}
