using System;
using Lucene.Net.Store;
using LuceneDirectory = Lucene.Net.Store.Directory;
using Nuget.PackageIndex.Logging;
using Nuget.PackageIndex.Engine;

namespace Nuget.PackageIndex
{
    /// <summary>
    /// Overrides PackageIndexBase and provides Directory and Analyzer for local index (located on
    /// user's machine). Local index contains types from packages located under:
    ///     - \Program Files (x86)\Microsoft Web Tools\Packages
    ///     - %UserProfile%\.k\packages
    /// </summary>
    public class LocalPackageIndex : PackageIndexBase
    {
        private readonly LuceneDirectory _directory;
        private readonly IPackageSearchEngine _engine;

        private readonly string _location;

        public LocalPackageIndex(ILogger logger = null)
        {
            Logger = logger;

            _location = Environment.ExpandEnvironmentVariables(@"%UserProfile%\AppData\Local\Microsoft\PackageIndex");
            if (!System.IO.Directory.Exists(_location))
            {
                System.IO.Directory.CreateDirectory(_location);
            }

            _directory = FSDirectory.Open(_location);
            _engine = new PackageSearchEngine(IndexDirectory, Analyzer, Logger);
        }

        /// <summary>
        /// ctor for unit tests
        /// </summary>
        internal LocalPackageIndex(LuceneDirectory directory, IPackageSearchEngine engine, ILogger logger)
        {
            _directory = directory;
            _engine = engine;
            Logger = logger;
        }

        #region PackageIndexBase

        protected override LuceneDirectory IndexDirectory
        {
            get
            {
                return _directory;
            }
        }

        protected override IPackageSearchEngine Engine
        {
            get
            {
                return _engine;
            }
        }

        #endregion
    }
}
