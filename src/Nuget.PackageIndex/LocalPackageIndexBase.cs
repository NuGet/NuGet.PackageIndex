using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using NuGet;
using Lucene.Net.Analysis;
using Lucene.Net.Search;
using Lucene.Net.Index;
using Mono.Cecil;
using LuceneDirectory = Lucene.Net.Store.Directory;
using Nuget.PackageIndex.Engine;
using Nuget.PackageIndex.Models;
using Nuget.PackageIndex.Logging;

namespace Nuget.PackageIndex
{
    /// <summary>
    /// Base class representing a package index, should be overriden with concrete index 
    /// implementation (local, remote etc)
    /// </summary>
    public abstract class LocalPackageIndexBase : ILocalPackageIndex, IDisposable
    {
        protected const int MaxTypesExpected = 5;
        private static readonly object _directoryLock = new object();
        private bool _disposed;

        private ILog _logger;
        protected ILog Logger {
            get
            {
                if (_logger == null)
                {
                    // if no logger specified - just create silent default logger
                    //var factory = new LoggerFactory();
                    //_logger = factory.Create(typeof(LocalPackageIndex).FullName);
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

        #region IPackageIndex

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
                return new DateTime(IndexReader.LastModified(IndexDirectory));
            }
        }

        public IList<PackageIndexError> AddPackage(ZipPackage package, bool force = false)
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
                if (existingPackageVersion >= package.Version && !force)
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

            Logger.WriteInformation("Adding package {0} {1} to index.",  package.Id, package.Version);
            var libFiles = package.GetLibFiles().Where(x => ".dll".Equals(Path.GetExtension(x.EffectivePath), StringComparison.OrdinalIgnoreCase));
            var uniqueAssemblies = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            var packageTypes = new List<TypeModel>();
            // get a list of all public types in all unique package assemblies
            foreach (IPackageFile dll in libFiles)
            {
                bool processed = false;
                if (uniqueAssemblies.TryGetValue(dll.EffectivePath, out processed))
                {
                    continue;
                }

                uniqueAssemblies.Add(dll.EffectivePath, true);
                // if there is a contracts assembly - take it, otherwise use normal assembly
                var dllToLoad = libFiles.FirstOrDefault(x => x.Path.ToLower().Contains(@"lib\contract")
                                        && x.EffectivePath.Equals(dll.EffectivePath, StringComparison.OrdinalIgnoreCase)) ?? dll;
                Logger.WriteVerbose("Processing assembly {0}.", dllToLoad.Path);
                var assemblyTypes = ProcessAssembly(package.Id, package.Version.ToString(), dllToLoad.GetStream());
                if (assemblyTypes != null)
                {
                    Logger.WriteVerbose("Found {0} public types.", assemblyTypes.Count());
                    packageTypes.AddRange(assemblyTypes);
                }
            }

            Logger.WriteVerbose("Storing package model to the index.");
            // add a pckage entry to index
            var result = Engine.AddEntry(
                new PackageModel
                {
                    Name = package.Id,
                    Version = package.Version.ToString()
                });

            Logger.WriteVerbose("Storing type models to the index.");
            // add all types to index
            result.AddRange(Engine.AddEntries(packageTypes, true));
            Logger.WriteVerbose("Package indexing complete.");

            return result;
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

            // remove package(s) from index
            var packages = GetPackages(packageName);
            errors.AddRange(Engine.RemoveEntries(packages, true)); // last one call optimize=true

            Logger.WriteVerbose("Package removed, '{0}' errors occured.", packageName, errors.Count());

            return errors;
        }

        public IList<PackageModel> GetPackages(string packageName)
        {
            return Engine.Search(new TermQuery(new Term(PackageModel.PackageNameField, packageName)), MaxTypesExpected)
                         .Select(x => new PackageModel(x)).ToList();

        }

        public IList<TypeModel> GetTypes(string typeName)
        {
            return Engine.Search(new TermQuery(new Term(TypeModel.TypeNameField, typeName)), MaxTypesExpected)
                         .Select(x => new TypeModel(x)).ToList();
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

        internal IList<TypeModel> ProcessAssembly(string packageId, string packageVersion, Stream assemblyStream)
        {
            string assemblyName = "";
            try
            {
                var typeEntities = new List<TypeModel>();

                var assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyStream);
                assemblyName = assemblyDefinition.Name.Name;
                foreach (var type in assemblyDefinition.MainModule.GetTypes())
                {
                    if (!type.IsPublic)
                    {
                        continue;
                    }

                    typeEntities.Add(new TypeModel
                    {
                        Name = type.Name,
                        FullName = type.FullName,
                        AssemblyName = assemblyName,
                        PackageName = packageId,
                        PackageVersion = packageVersion
                    });
                }

                return typeEntities;
            }
            catch(Exception ex)
            {
                Logger.WriteError("Types discovery error: {0}. Package: {1} {2}, assembly: {3}", ex.Message, packageId, packageVersion, assemblyName);
            }

            return null;
        }

        #region IDisposable 

        ~LocalPackageIndexBase()
        {
            Dispose();
        }

        public void Dispose()
        {
            lock (_directoryLock)
            {
                if (!_disposed)
                {
                    var engine = Engine;
                    if (engine != null)
                    {
                        try
                        {
                            engine.Dispose();
                        }
                        catch (ObjectDisposedException e)
                        {
                            _logger.WriteError("Failed to dispose engine. Exception: {0}", e.Message);
                        }
                    }

                    // proceed with local copy 
                    var directory = IndexDirectory;
                    if (directory != null)
                    {
                        try
                        {
                            directory.Dispose();
                        }
                        catch (ObjectDisposedException e)
                        {
                            _logger.WriteError("Failed to dispose index directory. Exception: {0}", e.Message);
                        }
                    }

                    _disposed = true;
                }
            }

            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
