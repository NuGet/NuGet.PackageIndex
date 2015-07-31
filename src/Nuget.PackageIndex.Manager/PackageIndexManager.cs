// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
                    result = builder.BuildAsync(shouldClean:false, newOnly: false, cancellationToken: CancellationToken.None).Result;
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
                    else if (!string.IsNullOrEmpty(arguments.Namespace))
                    {
                        DoQuery(() => { return builder.Index.GetNamespaces(arguments.Namespace).Select(x => (object)x).ToList(); });
                    }
                    else if (!string.IsNullOrEmpty(arguments.Extension))
                    {
                        DoQuery(() => { return builder.Index.GetExtensions(arguments.Extension).Select(x => (object)x).ToList(); });
                    }
                    else
                    {
                        _consoleUI.WriteNormalLine(Resources.PackageOrTypeIsMissing);
                    }
                    break;
                case PackageIndexManagerAction.Dump:
                    var dumpFile = arguments.DumpFile;
                    if (string.IsNullOrEmpty(dumpFile))
                    {
                        dumpFile = "IndexDump.csv";
                    }

                    DumpIndex(builder, dumpFile);
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

        private void DumpIndex(ILocalPackageIndexBuilder builder, string dumpFile)
        {
            if (File.Exists(dumpFile))
            {
                File.Delete(dumpFile);
            }

            StringBuilder stringBuilder = new StringBuilder();
            var allTypes = builder.Index.GetTypes().OrderBy(x => x.PackageName);
            stringBuilder.AppendLine("Types,,,,");
            foreach (var type in allTypes)
            {
                stringBuilder.AppendLine(string.Format("{0},{1},{2},{3},{4}", type.PackageName, type.PackageVersion, type.AssemblyName, type.FullName, string.Join(";", type.TargetFrameworks)));
            }

            var allExtensions = builder.Index.GetExtensions().OrderBy(x => x.PackageName);
            stringBuilder.AppendLine("Extensions,,,,");
            foreach (var extension in allExtensions)
            {
                stringBuilder.AppendLine(string.Format("{0},{1},{2},{3},{4}", extension.PackageName, extension.PackageVersion, extension.AssemblyName, extension.FullName, string.Join(";", extension.TargetFrameworks)));
            }

            var allNamespaces = builder.Index.GetNamespaces().OrderBy(x => x.PackageName);
            stringBuilder.AppendLine("Namespaces,,,,");
            foreach (var ns in allNamespaces)
            {
                stringBuilder.AppendLine(string.Format("{0},{1},{2},{3},{4}", ns.PackageName, ns.PackageVersion, ns.AssemblyName, ns.Name, string.Join(";", ns.TargetFrameworks)));
            }

            File.WriteAllText(dumpFile, stringBuilder.ToString());

            _consoleUI.WriteNormalLine(string.Format(Resources.DumpMessage, Path.GetFullPath(dumpFile)));
        }
    }
}
