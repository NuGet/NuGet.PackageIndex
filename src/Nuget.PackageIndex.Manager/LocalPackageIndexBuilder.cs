using NuGet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Nuget.PackageIndex;
using Nuget.PackageIndex.Models;
using ILogger = Nuget.PackageIndex.Logging.ILogger;

namespace Nuget.PackageIndex.Manager
{
    public class LocalPackageIndexBuilder : IDisposable
    {
        private static readonly object _indexLock = new object();
        private bool _disposed;

        // TODO use also KRE_HOME and other environment variables
        private List<string> DefaultSources = new List<string>()
            {
                @"%ProgramFiles(x86)%\Microsoft Web Tools\Packages",
                @"%UserProfile%\.k\packages"
            };
        private readonly IPackageIndex _index;
        private readonly ILogger _logger;
        private DateTime _lastIndexUpdateTime;

        public LocalPackageIndexBuilder(ILogger logger)
            :this(new LocalPackageIndex(logger), logger)
        {
        }

        internal LocalPackageIndexBuilder(IPackageIndex index, ILogger logger)
        {
            _index = index;
            _logger = logger;
            _lastIndexUpdateTime = DateTime.MinValue;
        }

        public IPackageIndex Index
        {
            get
            {
                return _index;
            }
        }

        public void ProcessAction(Arguments arguments, bool commandMode)
        {
            switch(arguments.Action)
            {
                case PackageIndexManagerAction.Build:
                    Build();
                    break;
                case PackageIndexManagerAction.Rebuild:
                    Rebuild();
                    break;
                case PackageIndexManagerAction.Clean:
                    Clean();
                    break;
                case PackageIndexManagerAction.Monitor:
                    break;
                case PackageIndexManagerAction.Add:
                    if (!string.IsNullOrEmpty(arguments.Package))
                    {
                        AddPackage(arguments.Package, arguments.Force);
                    }
                    else
                    {
                        ConsoleHelper.WriteNormalLine("Please specify package to be added to the index.");
                    }
                    break;
                case PackageIndexManagerAction.Remove:
                    if (!string.IsNullOrEmpty(arguments.Package))
                    {
                        RemovePackage(arguments.Package);
                    }
                    else
                    {
                        ConsoleHelper.WriteNormalLine("Please specify package to be removed from index.");
                    }
                    break;
                case PackageIndexManagerAction.Query:
                    if (!string.IsNullOrEmpty(arguments.Type))
                    {
                        DoQuery(() => { return Index.GetTypes(arguments.Type).Select(x => (IPackageIndexModel)x).ToList(); });
                    }
                    else if (!string.IsNullOrEmpty(arguments.Package))
                    {
                        DoQuery(() => { return Index.GetPackages(arguments.Package).Select(x => (IPackageIndexModel)x).ToList(); });
                    } else
                    {
                        ConsoleHelper.WriteNormalLine("Please specify package or type to be queried from index.");
                    }
                    break;
                default:
                    if (commandMode)
                    {
                        ConsoleHelper.WriteNormalLine("Unrecognized arguments...");
                    }
                    return;
            }
        }

        private void DoQuery(Func<IList<IPackageIndexModel>> queryExecutor)
        {
            var stopWatch = Stopwatch.StartNew();
            var entities = queryExecutor.Invoke();
            stopWatch.Stop();

            if (entities == null || entities.Count == 0)
            {
                ConsoleHelper.WriteNormalLine("No results ...");
                return;
            }

            foreach (var entity in entities)
            {
                ConsoleHelper.WriteNormalLine(entity.ToString());
            }

            ConsoleHelper.WriteHighlitedLine(string.Format("Time elapsed: {0}\r\n", stopWatch.Elapsed));
        }

        /// <summary>
        /// Getting packages from local folders that contain packages. By default they are:
        ///     - \Program Files (x86)\Microsoft Web Tools\Packages
        ///     - %UserProfile%\.k\packages
        /// </summary>
        private IEnumerable<string> GetPackages(bool newOnly)
        {
            string sources = string.Join(";", DefaultSources);
            _logger.WriteVerbose("Checking packages at: {0}", sources);

            foreach (var source in DefaultSources)
            {
                var expandedSource = Environment.ExpandEnvironmentVariables(source);
                var nupkgFiles = Directory.GetFiles(expandedSource, "*.nupkg", SearchOption.AllDirectories);
                foreach (var nupkgFile in nupkgFiles)
                {
                    if (newOnly && File.GetLastWriteTime(nupkgFile) <= _lastIndexUpdateTime)
                    {
                        continue;
                    }

                    yield return nupkgFile;
                }
            }
        }

        public void Build(bool newOnly = false)
        {
            _logger.WriteInformation("Started building index.");
            var stopWatch = Stopwatch.StartNew();

            // now get all known packages and add them to index again
            var packagePaths = GetPackages(newOnly).ToList();
            _logger.WriteVerbose("Found {0} packages to be added to the index.", packagePaths.Count());
            foreach (var nupkgFilePath in packagePaths)
            {
                AddPackageInternal(nupkgFilePath);
            }

            _lastIndexUpdateTime = DateTime.Now;

            stopWatch.Stop();
            _logger.WriteInformation("Finished building index.");
            ConsoleHelper.WriteHighlitedLine(string.Format("Time elapsed: {0}\r\n", stopWatch.Elapsed));
        }

        public void Clean()
        {
            _logger.WriteInformation("Started cleaning index.");
            var stopWatch = Stopwatch.StartNew();

            _index.Clean();

            _lastIndexUpdateTime = DateTime.MinValue;
            stopWatch.Stop();
            _logger.WriteInformation("Finished cleaning index, index now is empty.");
            ConsoleHelper.WriteHighlitedLine(string.Format("Time elapsed: {0}\r\n", stopWatch.Elapsed));
        }

        public void Rebuild()
        {
            Clean();
            Build();
        }

        public void AddPackage(string nupkgFilePath, bool force = false)
        {
            _logger.WriteInformation("Started package indexing {0}.", nupkgFilePath);
            var stopWatch = Stopwatch.StartNew();

            AddPackageInternal(nupkgFilePath, force);

            _lastIndexUpdateTime = DateTime.MinValue;
            stopWatch.Stop();
            _logger.WriteInformation("Finished package indexing.");
            ConsoleHelper.WriteHighlitedLine(string.Format("Time elapsed: {0}\r\n", stopWatch.Elapsed));
        }

        internal void AddPackageInternal(string nupkgFilePath, bool force = false)
        {
            if (string.IsNullOrEmpty(nupkgFilePath))
            {
                return;
            }

            if (!File.Exists(nupkgFilePath))
            {
                return;
            }

            ZipPackage package;
            using (var fs = File.OpenRead(nupkgFilePath))
            {
                package = new ZipPackage(fs);
            }

            _index.AddPackage(package, force);
        }

        public void RemovePackage(string packageName, bool force = false)
        {
            _logger.WriteInformation("Started package removing {0}.", packageName);
            var stopWatch = Stopwatch.StartNew();

            _index.RemovePackage(packageName);

            _lastIndexUpdateTime = DateTime.MinValue;
            stopWatch.Stop();
            _logger.WriteInformation("Finished package removing.");
            ConsoleHelper.WriteHighlitedLine(string.Format("Time elapsed: {0}\r\n", stopWatch.Elapsed));
        }

        #region IDisposable 

        ~LocalPackageIndexBuilder()
        {
            Dispose();
        }

        public void Dispose()
        {
            lock (_indexLock)
            {
                if (!_disposed)
                {
                    // proceed with local copy 
                    var index = _index;
                    if (index != null)
                    {
                        try
                        {
                            index.Dispose();
                        }
                        catch (ObjectDisposedException e)
                        {
                            _logger.WriteError("Failed to dispose index. Exception: {0}", e.Message);
                        }
                    }

                    _disposed = true;
                }
            }

            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
