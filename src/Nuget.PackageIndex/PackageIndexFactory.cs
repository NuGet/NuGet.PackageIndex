// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using Nuget.PackageIndex.Logging;

namespace Nuget.PackageIndex
{
    /// <summary>
    /// Is responsible for initialization of the indexes. If there no local index,
    /// it would schedule a task that will create an index on user machine.
    /// </summary>
    public class PackageIndexFactory : IPackageIndexFactory
    {
        // will be used to cancel all ongoing index update operations started asynchronously on background threads
        internal static CancellationTokenSource LocalIndexCancellationTokenSource = new CancellationTokenSource();

        private object _indexLock = new object();

        private ILog _logger;
        private static ILocalPackageIndex _index;

        public PackageIndexFactory()
            : this(new LogFactory(LogLevel.Quiet))
        {
        }

        public PackageIndexFactory(ILog logger)
        {
            _logger = logger;            
        }

        public CancellationToken GetCancellationToken()
        {
            return LocalIndexCancellationTokenSource.Token;
        }

        public void DetachFromLocalIndex()
        {
            LocalIndexCancellationTokenSource.Cancel();
        }

        /// <summary>
        /// TODO: remove create if not exist. Host should create index explicitly , however searchers should not create it
        /// </summary>
        public ILocalPackageIndex GetLocalIndex(bool createIfNotExists = true)
        {
            lock(_indexLock)
            {
                if (_index == null)
                {
                    _index = new LocalPackageIndex(_logger);

                    if (createIfNotExists)
                    {
                        var builder = new LocalPackageIndexBuilder(_index, _logger);
                        builder.BuildAsync(); // don't await - fire and forget
                    }
                }

                return _index;
            }
        }

        public IRemotePackageIndex GetRemoteIndex()
        {
            return null; // remote index is not implemented yet
        }

        /// <summary>
        /// Creating a new instance of builder, which would attach to existing local index instance
        /// </summary>
        /// <returns></returns>
        public ILocalPackageIndexBuilder GetLocalIndexBuilder(bool createIfNotExists = false)
        {
            return new LocalPackageIndexBuilder(GetLocalIndex(createIfNotExists), _logger);
        }
    }
}
