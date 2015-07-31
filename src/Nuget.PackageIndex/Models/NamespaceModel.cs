// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using System;
using System.Collections.Generic;

namespace Nuget.PackageIndex.Models
{
    /// <summary>
    /// Model for a public namespace found in a package's assemblies.
    /// </summary>
    internal class NamespaceModel : NamespaceInfo, IPackageIndexModel
    {
        internal const float NamespaceHashFieldBoost = 4f; // boost hash field more , since ythis is a primary ID
        internal const float NamespaceNameFieldBoost = 2f; // boost type name since this is the most common user search field
        internal const string NamespaceHashField = "NamespaceHash";
        internal const string NamespaceNameField = "NamespaceName";
        internal const string NamespaceAssemblyNameField = "NamespaceAssemblyName";
        internal const string NamespacePackageNameField = "NamespacePackageName";
        internal const string NamespacePackageVersionField = "NamespacePackageVersion";
        internal const string NamespaceTargetFrameworksField = "NamespaceTargetFrameworks";

        public NamespaceModel()
        {
            TargetFrameworks = new List<string>();
        }

        public NamespaceModel(Document document)
        {
            Name = document.Get(NamespaceNameField);
            AssemblyName = document.Get(NamespaceAssemblyNameField);
            PackageName = document.Get(NamespacePackageNameField);
            PackageVersion = document.Get(NamespacePackageVersionField);
            var targetFrameworksFieldValue = document.Get(NamespaceTargetFrameworksField);

            TargetFrameworks = new List<string>();
            if (!string.IsNullOrEmpty(targetFrameworksFieldValue))
            {
                TargetFrameworks.AddRange(targetFrameworksFieldValue.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
            }
        }

        public Document ToDocument()
        {
            var document = new Document();

            var typeHash = new Field(NamespaceHashField, ModelHelpers.GetMD5Hash(ToString()), Field.Store.NO, Field.Index.ANALYZED, Field.TermVector.NO);
            typeHash.Boost = NamespaceHashFieldBoost;
            var typeName = new Field(NamespaceNameField, Name, Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.NO);
            typeName.Boost = NamespaceNameFieldBoost;
            var typeAssemblyName = new Field(NamespaceAssemblyNameField, AssemblyName, Field.Store.YES, Field.Index.NO, Field.TermVector.NO);
            var typePackageName = new Field(NamespacePackageNameField, PackageName, Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.NO);
            var typePackageVersion = new Field(NamespacePackageVersionField, PackageVersion, Field.Store.YES, Field.Index.NO, Field.TermVector.NO);
            var typeTargetFrameworks = new Field(NamespaceTargetFrameworksField, GetTargetFrameworksString(), Field.Store.YES, Field.Index.NO, Field.TermVector.NO);

            document.Add(typeHash);
            document.Add(typeName);
            document.Add(typeAssemblyName);
            document.Add(typePackageName);
            document.Add(typePackageVersion);
            document.Add(typeTargetFrameworks);

            return document;
        }

        public Query GetDefaultSearchQuery()
        {
            return new TermQuery(new Term(NamespaceHashField, ModelHelpers.GetMD5Hash(ToString())));
        }
    }
}
