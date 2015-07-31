// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Lucene.Net.Documents;
using Lucene.Net.Search;
using Lucene.Net.Index;

namespace Nuget.PackageIndex.Models
{
    internal class PackageModel : PackageInfo, IPackageIndexModel
    {
        internal const float PackageNameFieldBoost = 2f; // boost name, since t is a primary ID
        internal const string PackageNameField = "PackageName";
        internal const string PackageVersionField = "PackageVersion";
        internal const string PackagePathField = "PackagePath";

        public object TypeHashField { get; private set; }

        public PackageModel()
        {
        }

        public PackageModel(Document document)
        {
            Name = document.Get(PackageNameField);
            Version = document.Get(PackageVersionField);
            Path = document.Get(PackagePathField);
        }

        public Document ToDocument()
        {
            var document = new Document();

            var packageName = new Field(PackageNameField, Name, Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.NO);
            packageName.Boost = PackageNameFieldBoost;
            var packageVersion = new Field(PackageVersionField, Version, Field.Store.YES, Field.Index.NO, Field.TermVector.NO);
            var packagePath = new Field(PackagePathField, Path, Field.Store.YES, Field.Index.NO, Field.TermVector.NO);

            document.Add(packageName);
            document.Add(packageVersion);
            document.Add(packagePath);

            return document;
        }

        public Query GetDefaultSearchQuery()
        {
            return new TermQuery(new Term(PackageNameField, Name));
        }

        public PackageInfo GetPackageInfo()
        {
            return new PackageInfo
            {
                Name = Name,
                Version = Version,
                Path = Path
            };
        }
    }
}
