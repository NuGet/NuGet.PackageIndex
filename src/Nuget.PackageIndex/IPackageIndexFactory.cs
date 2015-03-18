namespace Nuget.PackageIndex
{
    /// <summary>
    /// Is responsible for initialization of the local index. If there no local index,
    /// it would schedule a task that will create an index on user machine.
    /// </summary>
    public interface IPackageIndexFactory
    {
        ILocalPackageIndex GetLocalIndex(bool createIfNotExists = true);
        IRemotePackageIndex GetRemoteIndex();
        ILocalPackageIndexBuilder GetLocalIndexBuilder();
    }
}
