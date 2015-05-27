// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Nuget.PackageIndex.Models;

namespace Nuget.PackageIndex
{
    /// <summary>
    /// Retrieves types and methods information from a given assembly.
    /// When called for multiple assemblies in a package merges unique types 
    ///and other collected metadata.
    /// </summary>
    internal class RoslynReflector : IReflector
    {
        private Dictionary<string, TypeModel> _types;
        public IEnumerable<TypeModel> Types
        {
            get
            {
                return _types.Values;
            }
        }

        private Dictionary<string, NamespaceModel> _namespaces;
        public IEnumerable<NamespaceModel> Namespaces {
            get
            {
                return _namespaces.Values;
            }
        }

        private Dictionary<string, ExtensionModel> _extensions;
        public IEnumerable<ExtensionModel> Extensions {
            get
            {
                return _extensions.Values;
            }
        }

        private string _packageId;
        private string _packageVersion;
        private IEnumerable<string> _packageTargetFrameworks;

        public RoslynReflector(string packageId, string packageVersion, IEnumerable<string> packageTargetFrameworks)
        {
            _types = new Dictionary<string, TypeModel>();
            _namespaces = new Dictionary<string, NamespaceModel>();
            _extensions = new Dictionary<string, ExtensionModel>();
            _packageId = packageId;
            _packageVersion = packageVersion;
            _packageTargetFrameworks = packageTargetFrameworks;
        }

        /// <summary>
        /// Extracts types, extensions and namespaces data from given assembly and adds it to global
        /// collections to make sure we have unique data accross all assemblies in a package.
        /// </summary>
        public void ProcessAssembly(string assemblyPath)
        {
            try
            {
                var metadata = MetadataReference.CreateFromFile(assemblyPath);

                // create an empty CSharp compillation and add a single reference to given assembly.
                // Note: Even though we do use CSharp compillation here, we only se it to retrieve 
                // metadata, that should be common accross .Net assemblies (most of the times according to Roslyn)
                var compilation = CSharpCompilation.Create("dummy.dll", references: new[] { metadata });
                var assemblySymbol = (IAssemblySymbol)compilation.GetAssemblyOrModuleSymbol(metadata);

                ProcessNamespace(Path.GetFileName(assemblyPath), assemblySymbol.GlobalNamespace);
            }
            catch(Exception e)
            {
                Debug.Write(e.ToString());
            }
        }

        /// <summary>
        /// Reqursively goes through all namespaces in the assembly and discovers unique types, extensions and namespaces.
        /// </summary>
        private void ProcessNamespace(string assemblyName, INamespaceSymbol namespaceSymbol)
        {
            if (string.IsNullOrEmpty(assemblyName) || namespaceSymbol == null)
            {
                return;
            }

            var fullNamespaceName = (namespaceSymbol.ContainingNamespace == null || string.IsNullOrEmpty(namespaceSymbol.ContainingNamespace.Name))
                    ? namespaceSymbol.Name
                    : namespaceSymbol.ContainingNamespace + "." + namespaceSymbol.Name;

            foreach (var typeName in namespaceSymbol.GetTypeMembers())
            {
                try
                {
                    // keep only public types
                    if (typeName.DeclaredAccessibility != Accessibility.Public)
                    {
                        continue;
                    }

                    var typeFullName = string.IsNullOrEmpty(fullNamespaceName)
                                        ? typeName.Name
                                        : typeName.ContainingNamespace + "." + typeName.Name;

                    // if we meet this type first time - remember it
                    if (!_types.ContainsKey(typeFullName))
                    {
                        var newType = new TypeModel
                        {
                            Name = typeName.Name,
                            FullName = typeFullName,
                            AssemblyName = assemblyName,
                            PackageName = _packageId,
                            PackageVersion = _packageVersion
                        };
                        newType.TargetFrameworks.AddRange(_packageTargetFrameworks);

                        _types.Add(typeFullName, newType);

                        // if namespace contains at least one type and we meet this namespace first time - remember it
                        if (!string.IsNullOrEmpty(fullNamespaceName) && !_namespaces.ContainsKey(fullNamespaceName))
                        {
                            var newNamespace = new NamespaceModel
                            {
                                Name = fullNamespaceName,
                                AssemblyName = assemblyName,
                                PackageName = _packageId,
                                PackageVersion = _packageVersion
                            };
                            newNamespace.TargetFrameworks.AddRange(_packageTargetFrameworks);

                            _namespaces.Add(fullNamespaceName, newNamespace);
                        }
                    }

                    // if type is static, then it might contain extension methdos 
                    if (typeName.IsStatic)
                    {
                        foreach (var member in typeName.GetMembers())
                        {
                            // proceed only if static type's member is a method and it is public
                            var method = member as IMethodSymbol;
                            if (method == null || method.DeclaredAccessibility != Accessibility.Public)
                            {
                                continue;
                            }

                            if (method.IsExtensionMethod)
                            {
                                var thisParameter = method.Parameters[0];
                                var extensionMethodTypeName = thisParameter.Type.ContainingNamespace + "." + thisParameter.Type.Name;
                                var extensionFullName = extensionMethodTypeName + "." + method.Name;

                                // if we meet this extension first time - remember it
                                if (!_extensions.ContainsKey(extensionFullName))
                                {
                                    var newExtension = new ExtensionModel
                                    {
                                        Name = method.Name,
                                        FullName = extensionFullName,
                                        AssemblyName = assemblyName,
                                        PackageName = _packageId,
                                        PackageVersion = _packageVersion,
                                        Namespace = fullNamespaceName
                                    };
                                    newExtension.TargetFrameworks.AddRange(_packageTargetFrameworks);

                                    _extensions.Add(extensionFullName, newExtension);
                                }
                            }
                        }
                    }
                }
                catch(Exception e)
                {
                    Debug.Write(e.ToString());
                }
            }

            // recurse to chilren namespaces
            foreach (var childNamespace in namespaceSymbol.GetNamespaceMembers())
            {
                if (childNamespace.NamespaceKind == NamespaceKind.Module)
                {
                    ProcessNamespace(assemblyName, childNamespace);
                }
            }
        }
    }
}
