// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
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

        public IList<PackageIndexError> AddPackage(ZipPackage package, bool force = false)
        {
            try {
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

                Logger.WriteInformation("Adding package {0} {1} to index.", package.Id, package.Version);
                var libFiles = package.GetLibFiles().Where(x => ".dll".Equals(Path.GetExtension(x.EffectivePath), StringComparison.OrdinalIgnoreCase));
                var packageTargetFrameworks = package.GetSupportedFrameworks();
                var packageTypes = new Dictionary<string, TypeModel>(StringComparer.OrdinalIgnoreCase);
                var packageNamespaces = new Dictionary<string, NamespaceModel>(StringComparer.OrdinalIgnoreCase);

                // get a list of all public types in all unique package assemblies
                foreach (IPackageFile dll in libFiles)
                {
                    // if dll is a contracts dll it provides types accross all frameworks supported by package
                    var dllTtargetFrameworks = packageTargetFrameworks;
                    if (dll.TargetFramework != null && !dll.Path.ToLower().Contains(@"lib\contract"))
                    {
                        dllTtargetFrameworks = new[] { dll.TargetFramework };
                    }

                    Logger.WriteVerbose("Processing assembly {0}.", dll.Path);
                    var assemblyData = ProcessAssembly(package.Id, package.Version.ToString(), dllTtargetFrameworks, dll.GetStream());
                    if (assemblyData == null)
                    {
                        continue;
                    }

                    if (assemblyData.Types != null && assemblyData.Types.Any())
                    {
                        Logger.WriteVerbose("Found {0} public types.", assemblyData.Types.Count());
                        MergeTypes(packageTypes, assemblyData.Types);
                    }

                    //if (assemblyData.Namespaces != null && assemblyData.Namespaces.Any())
                    //{
                    //    Logger.WriteVerbose("Found {0} public namespaces.", assemblyData.Namespaces.Count());
                    //    MergeNamespaces(packageNamespaces, assemblyData.Namespaces);
                    //}
                }

                Logger.WriteVerbose("Storing package model to the index.");
                var result = Engine.AddEntry(
                    new PackageModel
                    {
                        Name = package.Id,
                        Version = package.Version.ToString()
                    });

                Logger.WriteVerbose("Storing type models to the index.");
                result.AddRange(Engine.AddEntries(packageTypes.Values, true));

                //Logger.WriteVerbose("Storing namespaces to the index.");
                //result.AddRange(Engine.AddEntries(packageNamespaces.Values, true));

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

            // remove package(s) from index
            var packages = GetPackagesInternal(packageName);
            errors.AddRange(Engine.RemoveEntries(packages, true)); // last one call optimize=true

            Logger.WriteVerbose("Package removed, '{0}' errors occured.", packageName, errors.Count());

            return errors;
        }

        private IList<PackageModel> GetPackagesInternal(string packageName)
        {
            return Engine.Search(new TermQuery(new Term(PackageModel.PackageNameField, packageName)), MaxTypesExpected)
                         .Select(x => new PackageModel(x)).ToList();
        }

        public IList<PackageInfo> GetPackages(string packageName)
        {
            return GetPackagesInternal(packageName).Select(x => (PackageInfo)x).ToList();
        }

        public IList<TypeInfo> GetTypes(string typeName)
        {
            return Engine.Search(new TermQuery(new Term(TypeModel.TypeNameField, typeName)), MaxTypesExpected)
                         .Select(x => (TypeInfo)new TypeModel(x)).ToList();
        }

        public IList<NamespaceInfo> GetNamespaces(string ns)
        {
            return Engine.Search(new TermQuery(new Term(NamespaceModel.NamespaceNameField, ns)), MaxTypesExpected)
                         .Select(x => (NamespaceInfo)new NamespaceModel(x)).ToList();
        }

        public IList<PackageIndexError> Clean()
        {
            return Engine.RemoveAll();
        }

        #endregion 

        private void MergeTypes(Dictionary<string, TypeModel> packageTypes, IEnumerable<TypeModel> assemblyTypes)
        {
            foreach (var newType in assemblyTypes)
            {
                TypeModel existingType = null;
                if (packageTypes.TryGetValue(newType.FullName, out existingType))
                {
                    foreach (var targetFramework in newType.TargetFrameworks)
                    {
                        // should we use a hashset instead of list here? There not so many targets: ~0 - 4 , 
                        // not sure if we would win anything ... consider it for future perf improvements.
                        if (existingType.TargetFrameworks.All(x => !x.Equals(targetFramework)))
                        {
                            existingType.TargetFrameworks.Add(targetFramework);
                        }
                    }
                }
                else
                {
                    packageTypes.Add(newType.FullName, newType);
                }
            }
        }

        private void MergeNamespaces(Dictionary<string, NamespaceModel> packageNamespaces, IEnumerable<NamespaceModel> assemblyNamespaces)
        {
            foreach (var newNamespace in assemblyNamespaces)
            {
                NamespaceModel existingNamespace = null;
                if (packageNamespaces.TryGetValue(newNamespace.Name, out existingNamespace))
                {
                    foreach (var targetFramework in newNamespace.TargetFrameworks)
                    {
                        // should we use a hashset instead of list here? There not so many targets: ~0 - 4 , 
                        // not sure if we would win anything ... consider it for future perf improvements.
                        if (existingNamespace.TargetFrameworks.All(x => !x.Equals(targetFramework)))
                        {
                            existingNamespace.TargetFrameworks.Add(targetFramework);
                        }
                    }
                }
                else
                {
                    packageNamespaces.Add(newNamespace.Name, newNamespace);
                }
            }
        }

        private IList<TypeModel> GetTypesInPackage(string packageName)
        {
            // new ConstantScoreQuery(TermFilter)
            // new Term(pq.Prefix.Field, pq.Prefix.Text + "*")
            return Engine.Search(new TermQuery(new Term(TypeModel.TypePackageNameField, packageName)))
                         .Select(x => new TypeModel(x)).ToList();
        }

        internal unsafe ProcessedAssemblyData ProcessAssembly(string packageId, string packageVersion, IEnumerable<FrameworkName> targetFrameworks, Stream assemblyStream)
        {
            var assemblyName = "";
            try
            {
                var targetFrameworkNames = targetFrameworks == null 
                    ? new List<string>() 
                    : targetFrameworks.Select(x => VersionUtility.GetShortFrameworkName(x));
                using (var peReader = new PEReader(assemblyStream))
                {
                    if (!peReader.HasMetadata)
                    {
                        return null;
                    }

                    var metadataBlock = peReader.GetMetadata();
                    if (metadataBlock.Pointer == (byte*)IntPtr.Zero || metadataBlock.Length <= 0)
                    {
                        return null;
                    }

                    var reader = new MetadataReader(metadataBlock.Pointer, metadataBlock.Length);
                    assemblyName = reader.GetString(reader.GetModuleDefinition().Name);

                    var typeHandlers = reader.TypeDefinitions;
                    var typeEntities = new List<TypeModel>();
                    var uniqueNamespaces = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var typeHandler in typeHandlers)
                    {
                        var typeDef = reader.GetTypeDefinition(typeHandler);

                        if ((typeDef.Attributes & System.Reflection.TypeAttributes.Public) != System.Reflection.TypeAttributes.Public)
                        {
                            continue;
                        }

                        var typeName = reader.GetString(typeDef.Name);
                        var typeNamespace = reader.GetString(typeDef.Namespace);

                        // remember unique namespaces
                        if (!string.IsNullOrEmpty(typeNamespace) && !uniqueNamespaces.Contains(typeNamespace))
                        {
                            uniqueNamespaces.Add(typeNamespace);
                        }

                        var newModel = new TypeModel
                        {
                            Name = typeName,
                            FullName = string.IsNullOrEmpty(typeNamespace) ? typeName : typeNamespace + "." + typeName,
                            AssemblyName = assemblyName,
                            PackageName = packageId,
                            PackageVersion = packageVersion
                        };

                        newModel.TargetFrameworks.AddRange(targetFrameworkNames);

                        typeEntities.Add(newModel);
                    }

                    //var methodHandlers = reader.MethodDefinitions;
                    //foreach (var methodHandler in methodHandlers)
                    //{
                    //    var methodDef = reader.GetMethodDefinition(methodHandler);

                    //    if ((methodDef.Attributes & System.Reflection.MethodAttributes.Public) != System.Reflection.MethodAttributes.Public)
                    //    {
                    //        continue;
                    //    }

                    //    var customAttributeHandles = methodDef.GetCustomAttributes();
                    //    foreach (var attrHandle in customAttributeHandles)
                    //    {
                    //        var attribute = reader.GetCustomAttribute(attrHandle);

                    //        if (attribute.Constructor.Kind == HandleKind.MemberReference) // MemberRef
                    //        {
                    //            var container = reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent;
                    //            var name = reader.GetTypeReference((TypeReferenceHandle)container).Name;
                    //            var attrName = reader.GetString(name);
                    //            if ("ExtensionAttribute".Equals(attrName))
                    //            {
                    //                // TODO 
                    //                var parHandle = methodDef.GetParameters().First();
                    //                var parameter = reader.GetParameter(parHandle);

                    //                // here parameter does not have a type information
                    //            }
                    //        }
                    //    }
                    //}

                    // if there were any namespaces generate their models
                    var namespaceEntities = new List<NamespaceModel>();
                    foreach (var ns in uniqueNamespaces)
                    {
                        var nsModel = new NamespaceModel
                        {
                            Name = ns,
                            AssemblyName = assemblyName,
                            PackageName = packageId,
                            PackageVersion = packageVersion
                        };

                        nsModel.TargetFrameworks.AddRange(targetFrameworkNames);

                        namespaceEntities.Add(nsModel);
                    }

                    return new ProcessedAssemblyData
                    {
                        Types = typeEntities,
                        Namespaces = namespaceEntities,
                        Extensions = null
                    };
                }             
            }
            catch(Exception ex)
            {
                Logger.WriteError("Types discovery error: {0}. Package: {1} {2}, assembly: {3}", ex.Message, packageId, packageVersion, assemblyName);
            }

            return null;
        }

        internal class ProcessedAssemblyData
        {
            public IList<TypeModel> Types { get; set; }
            public IList<NamespaceModel> Namespaces { get; set; }
            public IList<ExtensionModel> Extensions { get; set; }

        }
    }
}
