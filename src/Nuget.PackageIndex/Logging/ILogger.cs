namespace Nuget.PackageIndex.Logging
{
    /// <summary>
    /// Represents package index and exposes common operations for all index types (local, remote)
    /// </summary>
    public interface ILogger
    {
        void WriteVerbose(string format, params object[] args);
        void WriteInformation(string format, params object[] args);
        void WriteError(string format, params object[] args);
    }
}
