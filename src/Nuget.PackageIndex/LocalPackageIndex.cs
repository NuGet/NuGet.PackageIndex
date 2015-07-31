// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Lucene.Net.Store;
using LuceneDirectory = Lucene.Net.Store.Directory;
using Nuget.PackageIndex.Logging;
using Nuget.PackageIndex.Engine;

namespace Nuget.PackageIndex
{
    /// <summary>
    /// Overrides PackageIndexBase and provides Directory and Analyzer for local index (located on
    /// user's machine). Local index contains types from packages located under:
    ///     - \Program Files (x86)\Microsoft Web Tools\Packages
    ///     - %UserProfile%\.k\packages
    /// </summary>
    internal class LocalPackageIndex : LocalPackageIndexBase
    {
        private const string DefaultIndexPath = @"%UserProfile%\AppData\Local\Microsoft\PackageIndex";

        private readonly LuceneDirectory _directory;
        private readonly IPackageSearchEngine _engine;
        private readonly IReflectorFactory _reflectorFactory;

        private readonly string _location;

        public LocalPackageIndex(ILog logger = null)
        {
            Logger = logger;

            _location = Environment.ExpandEnvironmentVariables(DefaultIndexPath);
            if (!System.IO.Directory.Exists(_location))
            {
                System.IO.Directory.CreateDirectory(_location);
            }

            _directory = FSDirectory.Open(_location);
            _engine = new PackageSearchEngine(IndexDirectory, Analyzer, Logger);
            _reflectorFactory = new RoslynReflectorFactory();
        }

        /// <summary>
        /// ctor for unit tests
        /// </summary>
        internal LocalPackageIndex(LuceneDirectory directory, IPackageSearchEngine engine, IReflectorFactory reflectorFactory, ILog logger)
        {
            _directory = directory;
            _engine = engine;
            _reflectorFactory = reflectorFactory;
            Logger = logger;
        }

        #region PackageIndexBase

        protected override string IndexDirectoryPath
        {
            get
            {
                return _location;
            }
        }

        protected override LuceneDirectory IndexDirectory
        {
            get
            {
                return _directory;
            }
        }

        protected override IPackageSearchEngine Engine
        {
            get
            {
                return _engine;
            }
        }

        protected override DateTime GetLastWriteTime()
        {
            if (System.IO.Directory.Exists(_location))
            {
                return System.IO.Directory.GetLastWriteTime(_location);
            }

            return DateTime.MinValue;
        }

        protected override IReflectorFactory ReflectorFactory {
            get
            {
                return _reflectorFactory;
            }
        }

        #endregion
    }
}
