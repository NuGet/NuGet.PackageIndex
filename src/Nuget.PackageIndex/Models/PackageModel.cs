using System.Text;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using Lucene.Net.Index;

namespace Nuget.PackageIndex.Models
{
    public class PackageModel : IPackageIndexModel
    {
        internal const float PackageNameFieldBoost = 2f; // boost name, since t is a primary ID
        internal const string PackageNameField = "PackageName";
        internal const string PackageVersionField = "PackageVersion";

        public string Name { get; set; }
        public string Version { get; set; }
        public object TypeHashField { get; private set; }

        public PackageModel()
        {
        }

        public PackageModel(Document document)
        {
            Name = document.Get(PackageNameField);
            Version = document.Get(PackageVersionField);
        }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(Name)
                         .Append(Version);

            return stringBuilder.ToString();
        }

        public Document ToDocument()
        {
            var document = new Document();

            var packageName = new Field(PackageNameField, Name, Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.NO);
            packageName.Boost = PackageNameFieldBoost;
            var packageVersion = new Field(PackageVersionField, Version, Field.Store.YES, Field.Index.NO, Field.TermVector.NO);

            document.Add(packageName);
            document.Add(packageVersion);

            return document;
        }

        public Query GetDefaultSearchQuery()
        {
            return new TermQuery(new Term(PackageNameField, Name));
        }
    }
}
