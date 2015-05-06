using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nuget.PackageIndex.Engine;
using NuGet;
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
        {
            _logger = logger;
            _index = index;

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

        /// <summary>
        /// Getting packages from local folders that contain packages.
        /// </summary>
        public IEnumerable<string> GetPackages(bool newOnly, CancellationToken cancellationToken = default(CancellationToken))
        {
            _logger.WriteVerbose("Checking packages at: {0}", string.Join(";", _packageSources));

            var packages = new List<string>();
            foreach (var source in _packageSources)
            {
                var nupkgFiles = Directory.GetFiles(source, "*.nupkg", SearchOption.AllDirectories);
                foreach (var nupkgFile in nupkgFiles)
                {
                    if (cancellationToken != null && cancellationToken.IsCancellationRequested)
                    {
                        return null;
                    }

                    if (newOnly && File.GetLastWriteTime(nupkgFile) <= _index.LastWriteTime)
                    {
                        continue;
                    }

                    packages.Add(nupkgFile);
                }
            }

            return packages;
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

                var packagePaths = GetPackages(newOnly).ToList();
                _logger.WriteVerbose("Found {0} packages to be added to the index.", packagePaths.Count());
                bool success = true;
                foreach (var nupkgFilePath in packagePaths)
                {
                    if (cancellationToken != null && cancellationToken.IsCancellationRequested)
                    {
                        return new LocalPackageIndexBuilderResult { Success = false, TimeElapsed = stopWatch.Elapsed }; ;
                    }

                    var errors = AddPackageInternal(nupkgFilePath);
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

        public LocalPackageIndexBuilderResult AddPackage(string nupkgFilePath, bool force = false)
        {
            _logger.WriteInformation("Started package indexing {0}.", nupkgFilePath);
            var stopWatch = Stopwatch.StartNew();

            var errors = AddPackageInternal(nupkgFilePath, force);

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

        internal IList<PackageIndexError> AddPackageInternal(string nupkgFilePath, bool force = false)
        {
            if (string.IsNullOrEmpty(nupkgFilePath))
            {
                return null;
            }

            if (!File.Exists(nupkgFilePath))
            {
                return null;
            }

            ZipPackage package = null;
            try
            {
                using (var fs = File.OpenRead(nupkgFilePath))
                {
                    package = new ZipPackage(fs);
                }
            }
            catch(Exception e)
            {
                _logger.WriteError(string.Format("Failed to open package file '{0}'. Message: '{1}'", nupkgFilePath, e.Message));
            }

            return _index.AddPackage(package, force);
        }
    }
}
