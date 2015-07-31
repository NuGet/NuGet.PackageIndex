// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Nuget.PackageIndex.Models;
using Nuget.PackageIndex.Logging;

namespace Nuget.PackageIndex
{
    public class PackageSearcher : IPackageSearcher
    {
        private readonly IPackageIndexFactory _indexFactory;

        public PackageSearcher(ILog logger)
            : this(new PackageIndexFactory(logger))
        {
        }

        public PackageSearcher(IPackageIndexFactory indexFactory)
        {
            _indexFactory = indexFactory;
        }

        public IEnumerable<NamespaceInfo> SearchNamespace(string namespaceName)
        {
            IEnumerable<NamespaceInfo> result = null;
            var localIndex = _indexFactory.GetLocalIndex(createIfNotExists: true);
            if (localIndex != null)
            {
                result = localIndex.GetNamespaces(namespaceName);
            }

            if (result == null)
            {
                var remoteIndex = _indexFactory.GetRemoteIndex();
                if (remoteIndex != null)
                {
                    result = remoteIndex.GetNamespaces(namespaceName);
                }
            }

            return result;
        }

        public IEnumerable<ExtensionInfo> SearchExtension(string extensionName)
        {
            IEnumerable<ExtensionInfo> result = null;
            var localIndex = _indexFactory.GetLocalIndex(createIfNotExists: true);
            if (localIndex != null)
            {
                result = localIndex.GetExtensions(extensionName);
            }

            if (result == null)
            {
                var remoteIndex = _indexFactory.GetRemoteIndex();
                if (remoteIndex != null)
                {
                    result = remoteIndex.GetExtensions(extensionName);
                }
            }

            return result;
        }


        public IEnumerable<TypeInfo> SearchType(string typeName)
        {
            IEnumerable<TypeInfo> result = null;
            var localIndex = _indexFactory.GetLocalIndex(createIfNotExists:true);
            if (localIndex != null)
            {
                result = localIndex.GetTypes(typeName);
            }

            if (result == null)
            {
                var remoteIndex = _indexFactory.GetRemoteIndex();
                if (remoteIndex != null)
                {
                    result = remoteIndex.GetTypes(typeName);
                }
            }

            return result;
        }
    }
}
