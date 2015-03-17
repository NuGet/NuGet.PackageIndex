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

namespace Nuget.PackageIndex.Engine
{
    /// <summary>
    /// Given Lucene directory and anlyzer, wraps all CRUD operations for Lucene index. 
    /// By default is in Readonly mode, since most users of this class will be searching
    /// information (there could by multiple simultaneous readers). 
    /// Note: there must be only one process that writes to the index.
    /// </summary>
    public class PackageSearchEngine : IPackageSearchEngine, IDisposable
    {
        private static IndexWriter _writer;
        private static readonly object _writerLock = new object();
        private bool _disposed;

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
            foreach (var entry in entries)
            {
                try
                {
                    RemoveEntryInternal(entry);

                    var currentEntry = entry;
                    DoWriterAction(writer => writer.AddDocument(currentEntry.ToDocument()));
                }
                catch (Exception ex)
                {
                    errors.Add(new PackageIndexError(entry, ex));
                }
            }

            try
            {
                DoWriterAction(writer =>
                {
                    writer.Commit();
                    if (optimize)
                    {
                        writer.Optimize();
                    }
                });
            }
            catch (Exception ex)
            {
                // Note: if OutOfMemory we should call Close/Dispose for writer immediatelly!  
                errors.Add(new PackageIndexError(null, ex));
                _logger.WriteError("Failed to commit or optimize index writer. Exception: {0}", ex.Message);
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
            foreach (var entry in entries)
            {
                try
                {
                    RemoveEntryInternal(entry);
                }
                catch (Exception ex)
                {
                    errors.Add(new PackageIndexError(entry, ex));
                }
            }

            try
            {
                DoWriterAction(writer =>
                {
                    writer.Commit();
                    if (optimize)
                    {
                        writer.Optimize();
                    }
                });
            }
            catch (Exception ex)
            {
                errors.Add(new PackageIndexError(null, ex));
                _logger.WriteError("Failed to commit or optimize index writer. Exception: {0}", ex.Message);
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

        public IList<PackageIndexError> RemoveAll()
        {
            IList<PackageIndexError> errors = new List<PackageIndexError>();
            try
            {
                DoWriterAction(writer =>
                {
                    writer.DeleteAll();
                    writer.Commit();
                });
            }
            catch (Exception ex)
            {
                errors.Add(new PackageIndexError(null, ex));
                _logger.WriteError("Failed to remove all documents from index. Exception: {0}", ex.Message);
            }

            return errors;
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

        /// <summary>
        /// Thread safe wrapper around IndexWriter. Throws exception if in readonly mode.
        /// </summary>
        /// <param name="action">Action to be performed by writer</param>
        private void DoWriterAction(Action<IndexWriter> action)
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
                EnsureIndexWriter();
            }
            action(_writer);
        }

        /// <summary>
        /// Thread safe wrapper around IndexWriter. Throws exception if in readonly mode.
        /// </summary>
        /// <param name="action">Func to be performed by writer</param>
        private T DoWriterAction<T>(Func<IndexWriter, T> action)
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
                EnsureIndexWriter();
            }
            return action(_writer);
        }

        /// <summary>
        /// Ensures singleton writer is initialized and ready to go.
        /// Note: should only be called from within a lock.
        /// </summary>
        private void EnsureIndexWriter()
        {
            if (_writer == null)
            {
                // TODO Handle unlock differently - we may have several processes runing this class 
                // not just threads on the same process, so we need to unlock/lock differently:
                //    - for example see what process put lock there and wait until process exists 
                //      or for some timeout etc.
                if (IndexWriter.IsLocked(_directory))
                {
                    _logger.WriteInformation("Something left a lock in the index folder: deleting it");
                    IndexWriter.Unlock(_directory);
                    _logger.WriteInformation("Lock Deleted... can proceed");
                }

                _writer = new IndexWriter(_directory, _analyzer, new IndexWriter.MaxFieldLength(256));
                _writer.SetMergePolicy(new LogDocMergePolicy(_writer));
                _writer.MergeFactor = 5;
            }
        }

        /// <summary>
        /// Removes given entry from the index, but not commits.
        /// </summary>
        /// <param name="entry">Entry to be removed</param>
        internal void RemoveEntryInternal(IPackageIndexModel entry)
        {
            DoWriterAction(writer => writer.DeleteDocuments(entry.GetDefaultSearchQuery()));
        }

        #region IDisposable 

        ~PackageSearchEngine()
        {
            Dispose();
        }

        public void Dispose()
        {
            lock (_writerLock)
            {
                if (!_disposed)
                {
                    // proceed with local copy 
                    var writer = _writer;
                    if (writer != null)
                    {
                        try
                        {
                            writer.Dispose();
                        }
                        catch (ObjectDisposedException e)
                        {
                            _logger.WriteError("Failed to dispose index writer. Exception: {0}", e.Message);
                        }
                        _writer = null;
                    }

                    _disposed = true;
                }
            }

            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
