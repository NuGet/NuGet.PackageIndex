using System;
using Nuget.PackageIndex.Logging;

namespace Nuget.PackageIndex.Manager
{
    internal class ConsoleLogger : ILogProvider
    {
        public void WriteVerbose(string format, params object[] args)
        {
            WriteLine(format, args);
        }

        public void WriteInformation(string format, params object[] args)
        {
            WriteLine(format, args);
        }

        public void WriteError(string format, params object[] args)
        {
            WriteLine(format, args);
        }

        private void WriteLine(string format, params object[] args)
        {
            Console.WriteLine(string.Format(format, args));
        }
    }
}
