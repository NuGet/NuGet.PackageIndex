using System.Collections.Generic;

namespace Nuget.PackageIndex.Client
{
    /// <summary>
    /// Given a current file, returns a list of target frameworks for project 
    /// to which file belongs.
    /// </summary>
    public interface ITargetFrameworkProvider
    {
        IEnumerable<TargetFrameworkMetadata> GetTargetFrameworks(string filePath);
    }
}
