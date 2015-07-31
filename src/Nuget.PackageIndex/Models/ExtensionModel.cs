// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;

namespace Nuget.PackageIndex.Models
{
    /// <summary>
    /// Model for a public type found in a package's assemblies.
    /// </summary>
    internal class ExtensionModel : ExtensionInfo, IPackageIndexModel
    {
        internal const float ExtensionHashFieldBoost = 4f; // boost hash field more , since ythis is a primary ID
        internal const float ExtensionNameFieldBoost = 2f; // boost type name since this is the most common user search field
        internal const string ExtensionHashField = "ExtensionHash";
        internal const string ExtensionNameField = "ExtensionName";
        internal const string ExtensionFullNameField = "ExtensionFullName";
        internal const string ExtensionNamespaceField = "ExtensionNamespace";
        internal const string ExtensionAssemblyNameField = "ExtensionAssemblyName";
        internal const string ExtensionPackageNameField = "ExtensionPackageName";
        internal const string ExtensionPackageVersionField = "ExtensionPackageVersion";
        internal const string ExtensionTargetFrameworksField = "ExtensionTargetFrameworks";

        public ExtensionModel()
        {
            TargetFrameworks = new List<string>();
        }

        public ExtensionModel(Document document)
        {
            Name = document.Get(ExtensionNameField);
            FullName = document.Get(ExtensionFullNameField);
            Namespace = document.Get(ExtensionNamespaceField);
            AssemblyName = document.Get(ExtensionAssemblyNameField);
            PackageName = document.Get(ExtensionPackageNameField);
            PackageVersion = document.Get(ExtensionPackageVersionField);
            var targetFrameworksFieldValue = document.Get(ExtensionTargetFrameworksField);

            var targetFrameworks = new List<string>();
            if (!string.IsNullOrEmpty(targetFrameworksFieldValue))
            {
                targetFrameworks.AddRange(targetFrameworksFieldValue.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
            }

            TargetFrameworks = targetFrameworks;
        }

        public Document ToDocument()
        {
            var document = new Document();

            var hash = new Field(ExtensionHashField, ModelHelpers.GetMD5Hash(ToString()), Field.Store.NO, Field.Index.ANALYZED, Field.TermVector.NO);
            hash.Boost = ExtensionHashFieldBoost;
            var name = new Field(ExtensionNameField, Name, Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.NO);
            name.Boost = ExtensionNameFieldBoost;
            var fullName = new Field(ExtensionFullNameField, FullName, Field.Store.YES, Field.Index.NO, Field.TermVector.NO);
            var ns = new Field(ExtensionNamespaceField, Namespace, Field.Store.YES, Field.Index.NO, Field.TermVector.NO);
            var assemblyName = new Field(ExtensionAssemblyNameField, AssemblyName, Field.Store.YES, Field.Index.NO, Field.TermVector.NO);
            var packageName = new Field(ExtensionPackageNameField, PackageName, Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.NO);
            var packageVersion = new Field(ExtensionPackageVersionField, PackageVersion, Field.Store.YES, Field.Index.NO, Field.TermVector.NO);
            var targetFrameworks = new Field(ExtensionTargetFrameworksField, GetTargetFrameworksString(), Field.Store.YES, Field.Index.NO, Field.TermVector.NO);

            document.Add(hash);
            document.Add(name);
            document.Add(fullName);
            document.Add(ns);
            document.Add(assemblyName);
            document.Add(packageName);
            document.Add(packageVersion);
            document.Add(targetFrameworks);

            return document;
        }

        public Query GetDefaultSearchQuery()
        {
            return new TermQuery(new Term(ExtensionHashField, ModelHelpers.GetMD5Hash(ToString())));
        }
    }
}
