// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Nuget.PackageIndex.Manager
{
    internal class Arguments
    {
        public static Arguments Load(string[] args)
        {
            var optionsSet = new OptionsSet();
            var arguments = new Arguments(optionsSet);

            optionsSet.AddOption("-a|--action=", Resources.ActionDescription, value =>
            {
                var action = PackageIndexManagerAction.Unknown;
                if (!string.IsNullOrEmpty(value))
                {
                    Enum.TryParse(value, true, out action);
                }
                arguments.Action = action;
            });
            optionsSet.AddOption("-t|--type=", Resources.TypeDescription, value => arguments.Type = value);
            optionsSet.AddOption("-p|--package=", Resources.PackageDescription, value => arguments.Package = value);
            optionsSet.AddOption("-n|--namespace=", Resources.TypeDescription, value => arguments.Namespace = value);
            optionsSet.AddOption("-e|--extension=", Resources.TypeDescription, value => arguments.Extension = value);
            optionsSet.AddOption("-df|--dumpfile=", Resources.TypeDescription, value => arguments.DumpFile = value);
            optionsSet.AddOption("-q|--quiet", Resources.QuietDescription, value => arguments.Quiet = (value != null));
            optionsSet.AddOption("-v|--verbose", Resources.VerboseDescription, value => arguments.Verbose = (value != null));
            optionsSet.AddOption("-f|--force", Resources.ForceDescription, value => arguments.Force = (value != null));
            optionsSet.AddOption("-?|--help", Resources.HelpDescription, value => arguments.ShouldHelp = (value != null));
            optionsSet.Parse(args);
            
            return arguments;
        }

        private OptionsSet _optionSet;

        public Arguments(OptionsSet optionSet)
        {
            _optionSet = optionSet;
        }

        public PackageIndexManagerAction Action { get; set; }
        public string Type { get; set; }
        public string Package { get; set; }
        public string Namespace { get; set; }
        public string Extension { get; set; }
        public bool Force { get; set; }
        public bool Quiet { get; set; }
        public bool Verbose { get; set; }
        public bool ShouldHelp { get; set; }
        public string DumpFile { get; set; }

        public void PrintHelpMessage()
        {
            _optionSet.PrintHelpMessage(Console.Out);
        }
    }
}
