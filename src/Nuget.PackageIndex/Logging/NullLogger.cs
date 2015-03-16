namespace Nuget.PackageIndex.Logging
{
    /// <summary>
    /// Represents package index and exposes common operations for all index types (local, remote)
    /// </summary>
    public class NullLogger : ILogger
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
