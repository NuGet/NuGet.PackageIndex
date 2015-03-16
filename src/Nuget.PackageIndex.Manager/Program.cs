using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Nuget.PackageIndex.Logging;

namespace Nuget.PackageIndex.Manager
{
    public class Program
    {
        public void Main(string[] args)
        {
            var aa = Assembly.LoadFile(@"C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\IDE\Extensions\Microsoft\Web Tools\ProjectK\Nuget.PackageIndex.Core.dll");
            File.WriteAllText(@"d:\temp\ind.txt", aa.FullName);

            if (args == null || args.Count() == 0)
            {
                while (true)
                {
                    ConsoleHelper.WriteIntro(":>");

                    var argsString = ConsoleHelper.ReadLine();
                    var newArgs = argsString.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    Console.WriteLine();
                    if (!ProcessArguments(Arguments.Load(newArgs), true))
                    {
                        return;
                    }
                    Console.WriteLine();
                } 
            }
            else
            {
                var arguments = Arguments.Load(args);
                ProcessArguments(arguments, false);
            }
        }

        private static bool ProcessArguments(Arguments arguments, bool commandMode)
        {
            if (arguments.ShouldExit)
            {
                return false;
            }

            if (arguments.ShouldHelp)
            {
                arguments.PrintHelpMessage();
            }

            using (var builder = GetBuilder(arguments))
            {
                builder.ProcessAction(arguments, commandMode);
            }

            return true;
        }

        private static LocalPackageIndexBuilder GetBuilder(Arguments arguments)
        {
            //var factory = new LoggerFactory();
            //var logLevel = LogLevel.Information;
            //if (arguments.Quiet)
            //{
            //    logLevel = LogLevel.Critical;
            //} else if (arguments.Verbose)
            //{
            //    logLevel = LogLevel.Verbose;
            //}

            //factory.AddConsole(logLevel);
            //var builder = new LocalPackageIndexBuilder(factory.Create("PackageIndex.Manager"));
            var builder = new LocalPackageIndexBuilder(new NullLogger());
            return builder;
        }
    }
}
