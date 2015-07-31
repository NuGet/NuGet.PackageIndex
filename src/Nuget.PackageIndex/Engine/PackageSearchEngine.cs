// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
    /// Given Lucene directory and anlyzer, wraps all CRUD operations for Lucene package index. 
    /// By default is in Readonly mode, since most users of this class will be searching information
    /// (there could by multiple simultaneous readers). 
    /// Note: there must be only one writer accross all processes and threads on a machine.
    /// </summary>
    internal class PackageSearchEngine : IPackageSearchEngine
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
        /// When in readonly mode, all attempts to call "writeable" methods would cause an exception
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

        /// <summary>
        /// Removes all records in the index
        /// </summary>
        /// <returns>List of indexing errors if any</returns>
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
            using (IndexSearcher searcher = GetSearcher())
            {
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
        }

        #endregion

        /// <summary>
        /// Initializes a searcher. If index does not exist returns null. Always creates a readonly searcher.
        /// Note: there can be any number of searchers/readers which all are thread safe, so we can just create
        /// new one each time we need to search something.
        /// </summary>
        /// <returns>Returns index searcher</returns>
        private IndexSearcher GetSearcher()
        {
            try
            {
                if (!IndexReader.IndexExists(_directory))
                {
                    return null;
                }

                return new IndexSearcher(_directory, true);
            }
            catch
            {
                return null; // just in case
            }
        }

        /// <summary>
        /// There could be only one writer active across all threads and processes running on the machine,
        /// since a writer instance locks directory and others can not write to it to prevent index corruption. 
        /// Thus here we make sure that we don't create writers if directory is already locked.
        /// </summary>
        /// <returns>New instance of index writer or null if directory is locked</returns>
        private IndexWriter GetIndexWriter()
        {
            if (IsReadonly)
            {
                throw new Exception("Search engine is in readonly mode, can not initialize writer.");
            }

            lock (_writerLock) // prevent several threads creating writer at the same time
            {
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
