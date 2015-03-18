using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Index;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using LuceneDirectory = Lucene.Net.Store.Directory;
using Nuget.PackageIndex.Models;
using Nuget.PackageIndex.Logging;
using System.IO;

namespace Nuget.PackageIndex.Engine
{
    /// <summary>
    /// Given Lucene directory and anlyzer, wraps all CRUD operations for Lucene index. 
    /// By default is in Readonly mode, since most users of this class will be searching
    /// information (there could by multiple simultaneous readers). 
    /// Note: there must be only one process that writes to the index.
    /// </summary>
    public class PackageSearchEngine : IPackageSearchEngine
    {
        private static readonly object _writerLock = new object();

        private readonly ILog _logger;
        private readonly LuceneDirectory _directory;
        private readonly Analyzer _analyzer;

        public PackageSearchEngine(LuceneDirectory directory, Analyzer analyzer, ILog logger, bool readOnly = false)
        {
            _logger = logger;
            _directory = directory;
            _analyzer = analyzer;
            IsReadonly = readOnly;
        }

        #region IPackageSearchEngine

        /// <summary>
        /// When in readonly mode all attempts to call "writeable" methods would cause an exception
        /// </summary>
        public bool IsReadonly { get; private set; }

        /// <summary>
        /// Adds an entry implementing IPackageIndexModel to the index
        /// </summary>
        /// <typeparam name="T">Should be implementation of IPackageIndexModel</typeparam>
        /// <param name="entry">Entry to be added to the index</param>
        /// <returns>List of indexing errors if any</returns>
        public IList<PackageIndexError> AddEntry<T>(T entry) where T : IPackageIndexModel
        {
            return AddEntries(new[] { entry }, false);
        }

        /// <summary>
        /// Adds a list of entries implementing IPackageIndexModel to the index
        /// </summary>
        /// <typeparam name="T">Should be implementation of IPackageIndexModel</typeparam>
        /// <param name="entries">Entries to be added to the index</param>
        /// <param name="optimize">Should index be optimized after entries are submited? Optimization 
        /// may take some time and its for caller to decide should it be done or not.</param>
        /// <returns>List of indexing errors if any</returns>
        public IList<PackageIndexError> AddEntries<T>(IEnumerable<T> entries, bool optimize) where T : IPackageIndexModel
        {
            IList<PackageIndexError> errors = new List<PackageIndexError>();

            try
            {
                using (var writer = GetIndexWriter())
                {
                    foreach (var entry in entries)
                    {
                        try
                        {
                            // delete old entry if it exists
                            writer.DeleteDocuments(entry.GetDefaultSearchQuery());

                            // add new entry
                            writer.AddDocument(entry.ToDocument());
                        }
                        catch (Exception ex)
                        {
                            errors.Add(new PackageIndexError(entry, ex));
                        }
                    }

                    writer.Commit();
                    if (optimize)
                    {
                        writer.Optimize();
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add(new PackageIndexError(null, ex));
                _logger.WriteError("Index write operation failed. Exception: '{0}'", ex.Message);
            }

            return errors;
        }

        /// <summary>
        /// Removes entry implementing IPackageIndexModel from the index
        /// </summary>
        /// <typeparam name="T">Should be implementation of IPackageIndexModel</typeparam>
        /// <param name="entry">Entry to be removed from the index</param>
        /// <returns>List of indexing errors if any</returns>
        public IList<PackageIndexError> RemoveEntry<T>(T entry) where T : IPackageIndexModel
        {
            return RemoveEntries(new[] { entry }, false);
        }

        /// <summary>
        /// Removes a list of entries implementing IPackageIndexModel from the index
        /// </summary>
        /// <typeparam name="T">Should be implementation of IPackageIndexModel</typeparam>
        /// <param name="entries">Entries to be removed from the index</param>
        /// <param name="optimize">Should index be optimized after entries are submited? Optimization 
        /// may take some time and its for caller to decide should it be done or not.</param>
        /// <returns>List of indexing errors if any</returns>
        public IList<PackageIndexError> RemoveEntries<T>(IEnumerable<T> entries, bool optimize) where T : IPackageIndexModel
        {
            IList<PackageIndexError> errors = new List<PackageIndexError>();

            try
            {
                using (var writer = GetIndexWriter())
                {
                    foreach (var entry in entries)
                    {
                        try
                        {
                            writer.DeleteDocuments(entry.GetDefaultSearchQuery());
                        }
                        catch (Exception ex)
                        {
                            errors.Add(new PackageIndexError(entry, ex));
                        }
                    }

                    writer.Commit();
                    if (optimize)
                    {
                        writer.Optimize();
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add(new PackageIndexError(null, ex));
                _logger.WriteError("Index write operation failed. Exception: '{0}'", ex.Message);
            }

            return errors;
        }

        public IList<PackageIndexError> RemoveAll()
        {
            IList<PackageIndexError> errors = new List<PackageIndexError>();

            try
            {
                using (var writer = GetIndexWriter())
                {
                    writer.DeleteAll();
                    writer.Commit();
                };
            }
            catch (Exception ex)
            {
                errors.Add(new PackageIndexError(null, ex));
                _logger.WriteError("Index write operation failed. Exception: '{0}'", ex.Message);
            }

            return errors;
        }

        /// <summary>
        /// Performs a search in the index for given query.
        /// </summary>
        /// <param name="query">Query to be executed over index</param>
        /// <param name="max">Maximal number of records to be returned. If it is 0, all records satisfying query are returned.</param>
        /// <returns></returns>
        public IList<Document> Search(Query query, int max = 0)
        {
            IndexSearcher searcher = GetSearcher();
            if (searcher == null)
            {
                return new List<Document>();
            }

            var docIds = new List<int>();
            if (max > 0)
            {
                TopDocs hits = searcher.Search(query, max);
                docIds.AddRange(hits.ScoreDocs.Select(x => x.Doc));
            }
            else
            {
                var collector = new AllResultsCollector();
                searcher.Search(query, collector);
                docIds.AddRange(collector.Docs);
            }

            var results = new List<Document>();
            foreach (var id in docIds)
            {
                var document = searcher.Doc(id);
                results.Add(document);
            }

            return results;
        }

        #endregion

        /// <summary>
        /// Initializes a searcher. If index does not exist returns null. Always creates a readonly searcher.
        /// </summary>
        /// <returns>Returns index searcher</returns>
        private IndexSearcher GetSearcher()
        {
            try
            {
                // TODO check multithreading scenario here
                return new IndexSearcher(_directory, true);
            }
            catch(System.IO.FileNotFoundException)
            {
                return null; // index is not created yet
            }
        }

        private IndexWriter GetIndexWriter()
        {
            if (IsReadonly)
            {
                // Note: there should be only one writer that writes to the directory, since writers 
                // put lock file in directory. Even though in our EnsureIdexWriter we can unlock 
                // directory, this would work only if there no any otther process locking directory.
                // So basically we can handle multithreading in one process, but can not have several 
                // processes writing to the directory.
                // That's said we want to throw here to make sure users of this class are aware of 
                // this fact and if engine is explicitly in readonly mode it is assumed to do only 
                // search and should never attempts calling writer.
                throw new Exception("Search engine is in readonly mode, can not perform this action.");
            }

            lock (_writerLock)
            {
                // lock writer initialization to prevent several writers created by different threads
                if (IndexWriter.IsLocked(_directory))
                {                    
                    throw new Exception("Index is locked, some other write operation is in progress.");
                }

                var writer = new IndexWriter(_directory, _analyzer, new IndexWriter.MaxFieldLength(256));
                writer.SetMergePolicy(new LogDocMergePolicy(writer));
                writer.MergeFactor = 5;
                return writer;
            }
        }
    }
}
