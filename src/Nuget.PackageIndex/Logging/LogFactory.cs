using System.Collections.Generic;
using System.IO;

namespace Nuget.PackageIndex.Logging
{
    public class LogFactory : ILog
    {
        private List<ILogProvider> _logProviders;
        public LogFactory(LogLevel level)
        {
            Level = level;
            _logProviders = new List<ILogProvider>();
        }

        public LogLevel Level { get; set; }

        public void AddProvider(ILogProvider provider)
        {
            if (provider != null)
            {
                _logProviders.Add(provider);
            }
        }

        public void WriteVerbose(string format, params object[] args)
        {
            if (Level >= LogLevel.Verbose)
            {
                try
                {
                    _logProviders.ForEach(x => x.WriteVerbose(format, args));
                }
                catch(IOException)
                {
                    // in case of IO exception we just catch it here and ignore for now
                }
            }
        }

        public void WriteInformation(string format, params object[] args)
        {
            if (Level >= LogLevel.Information)
            {
                try
                {
                    _logProviders.ForEach(x => x.WriteInformation(format, args));
                }
                catch (IOException)
                {
                    // in case of IO exception we just catch it here and ignore for now
                }
            }
        }

        public void WriteError(string format, params object[] args)
        {
            if (Level >= LogLevel.Error)
            {
                try
                {
                    _logProviders.ForEach(x => x.WriteError(format, args));
                }
                catch (IOException)
                {
                    // in case of IO exception we just catch it here and ignore for now
                }
            }
        }
    }
}
