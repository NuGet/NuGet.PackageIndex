using System.Collections.Generic;
using Lucene.Net.Search;
using Lucene.Net.Index;

namespace Nuget.PackageIndex.Engine
{
    /// <summary>
    /// Lucene collector returning all matching records for a query.
    /// </summary>
    internal class AllResultsCollector : Collector
    {
        private int _docBase;

        private List<int> _docs = new List<int>();
        public List<int> Docs
        {
            get { return _docs; }
        }

        public override bool AcceptsDocsOutOfOrder
        {
            get
            {
                return true;
            }
        }

        public override void Collect(int doc)
        {
            _docs.Add(_docBase + doc);
        }

        public override void SetNextReader(IndexReader reader, int docBase)
        {
            _docBase = docBase;
        }

        public override void SetScorer(Scorer scorer)
        {
        }
    }
}
