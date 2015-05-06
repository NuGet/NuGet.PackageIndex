using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Nuget.PackageIndex.Logging;

namespace Nuget.PackageIndex.Manager
{
    internal class PackageIndexManager
    {
        private readonly IConsoleUI _consoleUI;

        public PackageIndexManager()
            :this(new ConsoleUI())
        {
        }

        public PackageIndexManager(IConsoleUI consoleUI)
        {
            _consoleUI = consoleUI;
        }

        public void Run(string[] args)
        {
            try
            {
                _consoleUI.WriteIntro(Resources.PackageIndexManagerTitle);
                _consoleUI.WriteNormalLine(string.Empty);

                var arguments = Arguments.Load(args);

                if (arguments.ShouldHelp)
                {
                    arguments.PrintHelpMessage();
                    return;
                }

                var builder = GetBuilder(arguments);
                ProcessAction(builder, arguments, false);
            }
            catch (Exception e)
            {
                _consoleUI.WriteNormalLine(e.Message);
            }
        }

        private ILocalPackageIndexBuilder GetBuilder(Arguments arguments)
        {
            var logLevel = LogLevel.Information;
            if (arguments.Quiet)
            {
                logLevel = LogLevel.Quiet;
            }
            else if (arguments.Verbose)
            {
                logLevel = LogLevel.Verbose;
            }

            var logFactory = new LogFactory(logLevel);
            logFactory.AddProvider(new ConsoleLogger());

            var indexFactory = new PackageIndexFactory(logFactory);

            return indexFactory.GetLocalIndexBuilder();
        }

        private void ProcessAction(ILocalPackageIndexBuilder builder, Arguments arguments, bool commandMode)
        {
            LocalPackageIndexBuilderResult result = null;
            switch (arguments.Action)
            {
                case PackageIndexManagerAction.Build:
                    result = builder.BuildAsync(newOnly:false, cancellationToken: CancellationToken.None).Result;
                    break;
                case PackageIndexManagerAction.Rebuild:
                    result = builder.Rebuild();
                    break;
                case PackageIndexManagerAction.Clean:
                    result = builder.Clean();
                    break;
                case PackageIndexManagerAction.Add:
                    if (!string.IsNullOrEmpty(arguments.Package))
                    {
                        result = builder.AddPackage(arguments.Package, arguments.Force);
                    }
                    else
                    {
                        _consoleUI.WriteNormalLine(Resources.PackageIsMissingToAdd);
                    }
                    break;
                case PackageIndexManagerAction.Remove:
                    if (!string.IsNullOrEmpty(arguments.Package))
                    {
                        result = builder.RemovePackage(packageName:arguments.Package, force: false);
                    }
                    else
                    {
                        _consoleUI.WriteNormalLine(Resources.PackageIsMissingToRemove);
                    }
                    break;
                case PackageIndexManagerAction.Query:
                    if (!string.IsNullOrEmpty(arguments.Type))
                    {
                        DoQuery(() => { return builder.Index.GetTypes(arguments.Type).Select(x => (object)x).ToList(); });
                    }
                    else if (!string.IsNullOrEmpty(arguments.Package))
                    {
                        DoQuery(() => { return builder.Index.GetPackages(arguments.Package).Select(x => (object)x).ToList(); });
                    }
                    else
                    {
                        _consoleUI.WriteNormalLine(Resources.PackageOrTypeIsMissing);
                    }
                    break;
                default:
                    _consoleUI.WriteNormalLine(Resources.ActionUnrecognized);
                    return;
            }

            if (result != null)
            {
                _consoleUI.WriteHighlitedLine(string.Format(Resources.TimeElapsed, result.TimeElapsed));
                _consoleUI.WriteNormalLine(string.Empty);
            }
        }

        private void DoQuery(Func<IList<object>> queryExecutor)
        {
            var stopWatch = Stopwatch.StartNew();
            var entities = queryExecutor.Invoke();
            stopWatch.Stop();

            if (entities == null || entities.Count == 0)
            {
                _consoleUI.WriteNormalLine(Resources.NoResults);
                return;
            }

            foreach (var entity in entities)
            {
                _consoleUI.WriteNormalLine(entity.ToString());
            }

            _consoleUI.WriteHighlitedLine(string.Format(Resources.TimeElapsed, stopWatch.Elapsed));
            _consoleUI.WriteNormalLine(string.Empty);
        }
    }
}
