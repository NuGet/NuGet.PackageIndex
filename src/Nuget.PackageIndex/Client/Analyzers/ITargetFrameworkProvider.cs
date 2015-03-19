using System.Collections.Generic;

namespace Nuget.PackageIndex.Client.Analyzers
{
    /// <summary>
    /// Given a current file, returns a list of target frameworks for project 
    /// to which file belongs.
    /// </summary>
    public interface ITargetFrameworkProvider
    {
        IEnumerable<string> GetTargetFrameworks(string filePath);
    }
}
