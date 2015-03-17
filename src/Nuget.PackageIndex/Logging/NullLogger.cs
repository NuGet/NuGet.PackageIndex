namespace Nuget.PackageIndex.Logging
{
    public class NullLogger : ILogProvider
    {
        public void WriteVerbose(string format, params object[] args)
        {
        }

        public void WriteInformation(string format, params object[] args)
        {
        }

        public void WriteError(string format, params object[] args)
        {
        }

    }
}
