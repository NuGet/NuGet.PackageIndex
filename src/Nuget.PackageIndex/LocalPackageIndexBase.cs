// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using NuGet;
using Lucene.Net.Analysis;
using Lucene.Net.Search;
using Lucene.Net.Index;
using LuceneDirectory = Lucene.Net.Store.Directory;
using Nuget.PackageIndex.Engine;
using Nuget.PackageIndex.Models;
using Nuget.PackageIndex.Logging;
using TypeInfo = Nuget.PackageIndex.Models.TypeInfo;

namespace Nuget.PackageIndex
{
    /// <summary>
    /// Base class representing a package index, should be overriden with concrete index 
    /// implementation (local, remote etc)
    /// </summary>
    internal abstract class LocalPackageIndexBase : ILocalPackageIndex
    {
        protected const int MaxTypesExpected = 5;

        private ILog _logger;
        protected ILog Logger {
            get
            {
                if (_logger == null)
                {
                    // if no logger specified - just create silent default logger
                    _logger = new LogFactory(LogLevel.Quiet);
                }

                return _logger;
            }
            set
            {
                _logger = value;
            }
        }

        private Analyzer _analyzer;
        protected virtual Analyzer Analyzer
        {
            get
            {
                if (_analyzer == null)
                {
                    _analyzer = new KeywordAnalyzer();
                }

                return _analyzer;
            }
        }

        protected abstract LuceneDirectory IndexDirectory { get; }
        protected abstract IPackageSearchEngine Engine { get; }
        protected abstract DateTime GetLastWriteTime();
        protected abstract string IndexDirectoryPath { get; }
        protected abstract IReflectorFactory ReflectorFactory{ get; }

        #region ILocalPackageIndex

        public string Location
        {
            get
            {
                return IndexDirectoryPath;
            }
        }

        public bool IsLocked
        {
            get
            {
                return IndexWriter.IsLocked(IndexDirectory);
            }
        }

        public bool IndexExists
        {
            get
            {
                return IndexReader.IndexExists(IndexDirectory);
            }
        }

        public DateTime LastWriteTime
        {
            get
            {
                return GetLastWriteTime();
            }
        }

        private IIndexSettings _settings;
        public IIndexSettings Settings
        {
            get
            {
                if (_settings == null)
                {
                    _settings = new IndexSettings(Location);
                }

                return _settings;
            }
        }

        public IList<PackageIndexError> AddPackage(IPackageMetadata package, bool force)
        {
            try
            {
                if (package == null)
                {
                    return null;
                }

                // check if package exists in the index:
                //  - if it does not, proceed and add it
                //  - if it is, add to index only if new package has higher version
                var existingPackage = GetPackages(package.Id).FirstOrDefault();
                if (existingPackage != null)
                {
                    var existingPackageVersion = new SemanticVersion(existingPackage.Version);
                    var newPackageVersion = new SemanticVersion(package.Version);
                    if (existingPackageVersion >= newPackageVersion && !force)
                    {
                        Logger.WriteVerbose("More recent version {0} of package {1} {2} exists in the index. Skipping...", existingPackageVersion.ToString(), package.Id, package.Version);
                        return new List<PackageIndexError>();
                    }
                    else
                    {
                        // Remove all old packages types. This is for the case if new package does not
                        // contain some types (deprecated), in this case index would still keep them.
                        Logger.WriteVerbose("Older version {0} of package {1} {2} exists in the index. Removing from index...", existingPackageVersion.ToString(), package.Id, package.Version);
                        RemovePackage(package.Id);
                    }
                }

                Logger.WriteInformation("Adding package {0} {1} to index.", package.Id, package.Version);


                var reflector = ReflectorFactory.Create(package);
                foreach (var assembly in package.Assemblies)
                {
                    Logger.WriteVerbose("Processing assembly {0}.", assembly.FullPath);
                    reflector.ProcessAssembly(assembly);
                }

                Logger.WriteVerbose("Storing package model to the index.");
                var result = Engine.AddEntry(
                    new PackageModel
                    {
                        Name = package.Id,
                        Version = package.Version.ToString(),
                        Path = package.LocalPath
                    });

                Logger.WriteVerbose("Storing type models to the index.");
                result.AddRange(Engine.AddEntries(reflector.Types, false));

                Logger.WriteVerbose("Storing namespaces to the index.");
                result.AddRange(Engine.AddEntries(reflector.Namespaces, false));

                Logger.WriteVerbose("Storing extensions to the index.");
                result.AddRange(Engine.AddEntries(reflector.Extensions, true));

                Logger.WriteVerbose("Package indexing complete.");

                return result;
            }
            catch(Exception e)
            {
                Debug.Write(e.ToString());
            }

            return null;
        }

        public IList<PackageIndexError> RemovePackage(string packageName)
        {
            if (string.IsNullOrEmpty(packageName))
            {
                throw new ArgumentNullException("packageName");
            }

            Logger.WriteInformation("Removing package {0} model and types from the index.", packageName);

            // remove all types from given package(s)
            var types = GetTypesInPackage(packageName);
            var errors = Engine.RemoveEntries(types, false);

            // remove all namespaces from given package(s)
            var namespaces = GetNamespacesInPackage(packageName);
            errors.AddRange(Engine.RemoveEntries(namespaces, false));

            // remove all namespaces from given package(s)
            var extensions = GetExtensionsInPackage(packageName);
            errors.AddRange(Engine.RemoveEntries(extensions, false));

            // remove package(s) from index
            var packages = GetPackagesInternal(packageName);
            errors.AddRange(Engine.RemoveEntries(packages, true)); // last one call optimize=true

            Logger.WriteVerbose("Package removed, '{0}' errors occured.", packageName, errors.Count());

            return errors;
        }

        public IList<PackageInfo> GetPackages()
        {
            return GetPackages(null);
        }

        public IList<PackageInfo> GetPackages(string packageName)
        {
            return GetPackagesInternal(packageName).Select(x => (PackageInfo)x).ToList();
        }

        public IList<TypeInfo> GetTypes()
        {
            return GetTypes(null);
        }

        public IList<TypeInfo> GetTypes(string typeName)
        {
            return GetTypesInternal(typeName).Select(x => (TypeInfo)x).ToList();
        }

        public IList<NamespaceInfo> GetNamespaces()
        {
            return GetNamespaces(null);
        }

        public IList<NamespaceInfo> GetNamespaces(string ns)
        {
            return GetNamespacesInternal(ns).Select(x => (NamespaceInfo)x).ToList();
        }

        public IList<ExtensionInfo> GetExtensions()
        {
            return GetExtensions(null);
        }

        public IList<ExtensionInfo> GetExtensions(string extension)
        {
            return GetExtensionsInternal(extension).Select(x => (ExtensionInfo)x).ToList();
        }

        public IList<PackageIndexError> Clean()
        {
            return Engine.RemoveAll();
        }

        #endregion 

        private IList<TypeModel> GetTypesInPackage(string packageName)
        {
            return Engine.Search(new TermQuery(new Term(TypeModel.TypePackageNameField, packageName)))
                         .Select(x => new TypeModel(x)).ToList();
        }

        private IList<NamespaceModel> GetNamespacesInPackage(string packageName)
        {
            return Engine.Search(new TermQuery(new Term(NamespaceModel.NamespacePackageNameField, packageName)))
                         .Select(x => new NamespaceModel(x)).ToList();
        }

        private IList<ExtensionModel> GetExtensionsInPackage(string packageName)
        {
            return Engine.Search(new TermQuery(new Term(ExtensionModel.ExtensionPackageNameField, packageName)))
                         .Select(x => new ExtensionModel(x)).ToList();
        }

        private IList<PackageModel> GetPackagesInternal(string packageName)
        {
            return GetModelsInternal(packageName, PackageModel.PackageNameField, (doc) => new PackageModel(doc))
                    .Select(x => (PackageModel)x).ToList();
        }

        private IList<TypeModel> GetTypesInternal(string typeName)
        {
            return GetModelsInternal(typeName, TypeModel.TypeNameField, (doc) => new TypeModel(doc))
                    .Select(x => (TypeModel)x).ToList();
        }

        private IList<NamespaceModel> GetNamespacesInternal(string namespaceName)
        {
            return GetModelsInternal(namespaceName, NamespaceModel.NamespaceNameField, (doc) => new NamespaceModel(doc))
                    .Select(x => (NamespaceModel)x).ToList();
        }

        private IList<ExtensionModel> GetExtensionsInternal(string extensionName)
        {
            return GetModelsInternal(extensionName, ExtensionModel.ExtensionNameField, (doc) => new ExtensionModel(doc))
                    .Select(x => (ExtensionModel)x).ToList();
        }

        private IList<IPackageIndexModel> GetModelsInternal(string modelName, string modelNameField, Func<Lucene.Net.Documents.Document, IPackageIndexModel> createModel)
        {
            int maxResults = 0;
            Query query = null;
            if (string.IsNullOrEmpty(modelName))
            {
                query = new WildcardQuery(new Term(modelNameField, "*"));
            }
            else
            {
                maxResults = MaxTypesExpected;
                query = new TermQuery(new Term(modelNameField, modelName));
            }

            return Engine.Search(query, maxResults)
                         .Select(x => createModel(x))
                         .ToList();
        }
    }
}
