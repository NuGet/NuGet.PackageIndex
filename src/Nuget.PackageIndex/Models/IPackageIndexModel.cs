using Lucene.Net.Documents;
using Lucene.Net.Search;

namespace Nuget.PackageIndex.Models
{
    /// <summary>
    /// Package Index model representation, all models must implement it if they want to be 
    /// stored in the index.
    /// </summary>
    internal interface IPackageIndexModel
    {
        Document ToDocument();
        Query GetDefaultSearchQuery();
    }
}
