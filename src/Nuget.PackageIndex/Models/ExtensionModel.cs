// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Nuget.PackageIndex.Models
{
    /// <summary>
    /// Model for a public type found in a package's assemblies.
    /// </summary>
    internal class ExtensionModel : TypeInfo, IPackageIndexModel
    {
        internal const float TypeHashFieldBoost = 4f; // boost hash field more , since ythis is a primary ID
        internal const float TypeNameFieldBoost = 2f; // boost type name since this is the most common user search field
        internal const string TypeHashField = "TypeHash";
        internal const string TypeNameField = "TypeName";
        internal const string TypeFullNameField = "TypeFullName";
        internal const string TypeAssemblyNameField = "TypeAssemblyName";
        internal const string TypePackageNameField = "TypePackageName";
        internal const string TypePackageVersionField = "TypePackageVersion";
        internal const string TypeTargetFrameworksField = "TypeTargetFrameworks";

        public static string GetMD5Hash(ExtensionModel typeEntity)
        {
            // convert entity to byte array
            var entityContents = typeEntity.ToString();
            var byteContainer = new byte[entityContents.Length * 2];
            Encoding.UTF8.GetEncoder().GetBytes(entityContents.ToCharArray(), 0, entityContents.Length, byteContainer, 0, true);

            // generate MD5 hash for byte array
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] result = md5.ComputeHash(byteContainer);

            // convert hash to hex string
            var stringBuilder = new StringBuilder();
            for (var i = 0; i < result.Length; i++)
            {
                stringBuilder.Append(result[i].ToString("X2"));
            }

            return stringBuilder.ToString();
        }

        public ExtensionModel()
        {
            TargetFrameworks = new List<string>();
        }

        public ExtensionModel(Document document)
        {
            Name = document.Get(TypeNameField);
            FullName = document.Get(TypeFullNameField);
            AssemblyName = document.Get(TypeAssemblyNameField);
            PackageName = document.Get(TypePackageNameField);
            PackageVersion = document.Get(TypePackageVersionField);
            var targetFrameworksFieldValue = document.Get(TypeTargetFrameworksField);

            TargetFrameworks = new List<string>();
            if (!string.IsNullOrEmpty(targetFrameworksFieldValue))
            {
                TargetFrameworks.AddRange(targetFrameworksFieldValue.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
            }
        }

        public Document ToDocument()
        {
            var document = new Document();

            var typeHash = new Field(TypeHashField, GetMD5Hash(this), Field.Store.NO, Field.Index.ANALYZED, Field.TermVector.NO);
            typeHash.Boost = TypeHashFieldBoost;
            var typeName = new Field(TypeNameField, Name, Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.NO);
            typeName.Boost = TypeNameFieldBoost;
            var typeFullName = new Field(TypeFullNameField, FullName, Field.Store.YES, Field.Index.NO, Field.TermVector.NO);
            var typeAssemblyName = new Field(TypeAssemblyNameField, AssemblyName, Field.Store.YES, Field.Index.NO, Field.TermVector.NO);
            var typePackageName = new Field(TypePackageNameField, PackageName, Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.NO);
            var typePackageVersion = new Field(TypePackageVersionField, PackageVersion, Field.Store.YES, Field.Index.NO, Field.TermVector.NO);
            var typeTargetFrameworks = new Field(TypeTargetFrameworksField, GetTargetFrameworksString(), Field.Store.YES, Field.Index.NO, Field.TermVector.NO);

            document.Add(typeHash);
            document.Add(typeName);
            document.Add(typeFullName);
            document.Add(typeAssemblyName);
            document.Add(typePackageName);
            document.Add(typePackageVersion);
            document.Add(typeTargetFrameworks);

            return document;
        }

        public Query GetDefaultSearchQuery()
        {
            return new TermQuery(new Term(TypeHashField, GetMD5Hash(this)));
        }
    }
}
