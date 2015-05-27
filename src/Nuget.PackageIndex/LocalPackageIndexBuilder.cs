// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using Nuget.PackageIndex.Engine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ILog = Nuget.PackageIndex.Logging.ILog;

namespace Nuget.PackageIndex
{
    public class LocalPackageIndexBuilder : ILocalPackageIndexBuilder
    {
        // TODO in order to get all default nuget sources on local machine,
        // we would need to have another method in IVsPackageInstallerService, some thing like
        // GetPreinstalledPackages sources etc, which would give us all local sources for all 
        // installed VS extensions
        // or 
        // we would need to request sources from IVsPackageInstallerServices based on project,
        // in this case should we have multiple local indexes? - i think one index is preferrable.
        private const string PackageSourcesEnvironmentVariable = "NugetLocalPackageSources";
        private List<string> DefaultSources = new List<string>()
            {
                @"%ProgramFiles(x86)%\Microsoft Web Tools\DNU",
                @"%ProgramFiles(x86)%\Microsoft Web Tools\Packages",
                @"%UserProfile%\.dnx\packages"
            };

        private List<string> _packageSources;
        private readonly ILocalPackageLoader _discoverer;
        private readonly ILocalPackageIndex _index;
        public ILocalPackageIndex Index
        {
            get
            {
                return _index;
            }
        }

        private readonly ILog _logger;

        public LocalPackageIndexBuilder(ILog logger)
            : this(new LocalPackageIndex(logger), logger)
        {
        }

        internal LocalPackageIndexBuilder(ILocalPackageIndex index, ILog logger)
            : this(index, logger, new NupkgLocalPackageLoader())
        {
        }

        internal LocalPackageIndexBuilder(ILocalPackageIndex index, ILog logger, ILocalPackageLoader discoverer)
        {
            _logger = logger;
            _index = index;
            _discoverer = discoverer;

            InitializePackageSources();
        }

        private void InitializePackageSources()
        {
            var sources = new List<string>(DefaultSources);
            var additionalSourcesVariableValue = Environment.GetEnvironmentVariable(PackageSourcesEnvironmentVariable);
            if (!string.IsNullOrEmpty(additionalSourcesVariableValue))
            {
                sources.AddRange(additionalSourcesVariableValue.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
            }

            _packageSources = sources.Select(x => Environment.ExpandEnvironmentVariables(x)).ToList();
        }

        public IEnumerable<string> GetPackageDirectories()
        {
            return DefaultSources.Select(x => Environment.ExpandEnvironmentVariables(x));
        }

        public Task<LocalPackageIndexBuilderResult> BuildAsync(bool newOnly = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.Run(() =>
            {
                // Fire and forget. While index is building, it will be locked from
                // other write attempts. In meanwhile readers would just not be able 
                // to find any types, but will be still operatable (when an instance of 
                // a reader is created it can return data from the snapshot before next
                // write happened).
                _logger.WriteInformation("Started building index.");
                var stopWatch = Stopwatch.StartNew();

                // now get all known packages and add them to index again
                if (newOnly)
                {
                    _logger.WriteVerbose("Looking only for new packages...");
                }
                else
                {
                    _logger.WriteVerbose("Looking all existing packages...");
                }

                var packages = _discoverer.DiscoverPackages(_packageSources, newOnly, _index.LastWriteTime, cancellationToken).ToList();
                _logger.WriteVerbose("Found {0} packages to be added to the index.", packages.Count());

                bool success = true;
                foreach (var package in packages)
                {
                    if (cancellationToken != null && cancellationToken.IsCancellationRequested)
                    {
                        return new LocalPackageIndexBuilderResult { Success = false, TimeElapsed = stopWatch.Elapsed }; ;
                    }

                    var errors = _index.AddPackage(package, force: false);
                    success &= (errors == null || !errors.Any());
                }

                stopWatch.Stop();
                _logger.WriteInformation("Finished building index.");

                return new LocalPackageIndexBuilderResult { Success = success, TimeElapsed = stopWatch.Elapsed };
            }, PackageIndexFactory.LocalIndexCancellationTokenSource.Token);
        }

        public LocalPackageIndexBuilderResult Clean()
        {
            _logger.WriteInformation("Started cleaning index.");
            var stopWatch = Stopwatch.StartNew();

            var errors =_index.Clean();

            stopWatch.Stop();
            _logger.WriteInformation("Finished cleaning index, index now is empty.");

            return new LocalPackageIndexBuilderResult { Success = errors == null || !errors.Any(), TimeElapsed = stopWatch.Elapsed };
        }

        public LocalPackageIndexBuilderResult Rebuild()
        {
            var stopWatch = Stopwatch.StartNew();
            bool success = Clean().Success && BuildAsync().Result.Success;
            stopWatch.Stop();

            return new LocalPackageIndexBuilderResult { Success = success, TimeElapsed = stopWatch.Elapsed };
        }

        public LocalPackageIndexBuilderResult AddPackage(string nupkgFilePath, bool force)
        {
            _logger.WriteInformation("Started package indexing {0}.", nupkgFilePath);
            var stopWatch = Stopwatch.StartNew();

            IList<PackageIndexError> errors = null;
            var package = _discoverer.GetPackageMetadataFromPath(nupkgFilePath);
            if (package != null)
            {
                errors = _index.AddPackage(package, force);
            }

            stopWatch.Stop();
            _logger.WriteInformation("Finished package indexing.");

            return new LocalPackageIndexBuilderResult { Success = errors == null || !errors.Any(), TimeElapsed = stopWatch.Elapsed };
        }

        public LocalPackageIndexBuilderResult RemovePackage(string packageName, bool force = false)
        {
            _logger.WriteInformation("Started package removing {0}.", packageName);
            var stopWatch = Stopwatch.StartNew();

            var errors =_index.RemovePackage(packageName);

            stopWatch.Stop();
            _logger.WriteInformation("Finished package removing.");

            return new LocalPackageIndexBuilderResult { Success = errors == null || !errors.Any(), TimeElapsed = stopWatch.Elapsed };
        }
    }
}
