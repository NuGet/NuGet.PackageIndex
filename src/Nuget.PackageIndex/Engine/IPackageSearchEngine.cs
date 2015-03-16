﻿using System;
using System.Collections.Generic;
using Lucene.Net.Search;
using Lucene.Net.Documents;
using Nuget.PackageIndex.Models;

namespace Nuget.PackageIndex.Engine
{
    /// <summary>
    /// Index search engine common interface.
    /// </summary>
    public interface IPackageSearchEngine : IDisposable
    {
        bool IsReadonly { get; }
        IList<PackageIndexError> AddEntry<T>(T entry) where T : IPackageIndexModel;
        IList<PackageIndexError> AddEntries<T>(IEnumerable<T> entries, bool optimize) where T : IPackageIndexModel;
        IList<PackageIndexError> RemoveEntry<T>(T entry) where T : IPackageIndexModel;
        IList<PackageIndexError> RemoveEntries<T>(IEnumerable<T> entries, bool optimize) where T : IPackageIndexModel;
        IList<PackageIndexError> RemoveAll();
        IList<Document> Search(Query query, int max = 0);
    }
}
