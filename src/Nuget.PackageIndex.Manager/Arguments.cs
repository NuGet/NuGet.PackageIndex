using System;
using NDesk.Options;

namespace Nuget.PackageIndex.Manager
{
    public enum PackageIndexManagerAction
    {
        Unknown,
        Monitor,
        Build,
        Rebuild,
        Clean,
        Add,
        Remove,
        Query
    }

    public class Arguments
    {
        public static Arguments Load(string[] args)
        {
            var optionsSet = new OptionSet();
            var arguments = new Arguments(optionsSet);

            optionsSet.Add("a|action=", "Action to be done with package index: Monitor, Build, Rebuild, Clean, Add, Remove, Query.", value =>
            {
                var action = PackageIndexManagerAction.Unknown;
                if (!string.IsNullOrEmpty(value))
                {
                    Enum.TryParse(value, true, out action);
                }
                arguments.Action = action;
            });
            optionsSet.Add("t|type=", "Query a type: type=YourType.", value => arguments.Type = value);
            optionsSet.Add("p|package=", "Query a package: package=YourPackageName.", value => arguments.Package = value);
            optionsSet.Add("q|quiet", "No output to the log or console.", value => arguments.Quiet = (value != null));
            optionsSet.Add("v|verbose", "Detailed output to the log or console.", value => arguments.Verbose = (value != null));
            optionsSet.Add("f|force", "Force selected action.", value => arguments.Force = (value != null));
            optionsSet.Add("x|exit", "Disconnect index and quit.", value => arguments.ShouldExit = (value != null));
            optionsSet.Add("?|help", "Display this message.", value => arguments.ShouldHelp = (value != null));
            optionsSet.Parse(args);

            return arguments;
        }

        private OptionSet _optionSet;

        public Arguments(OptionSet optionSet)
        {
            _optionSet = optionSet;
        }

        public PackageIndexManagerAction Action { get; set; }
        public string Type { get; set; }
        public string Package { get; set; }
        public bool Force { get; set; }
        public bool Quiet { get; set; }
        public bool Verbose { get; set; }
        public bool ShouldExit { get; set; }
        public bool ShouldHelp { get; set; }

        public void PrintHelpMessage()
        {
            Console.WriteLine("Package Index Monitor");
            Console.WriteLine();
            _optionSet.WriteOptionDescriptions(Console.Out);
        }
    }
}
