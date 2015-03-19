using System.Collections.Generic;

namespace Nuget.PackageIndex.VisualStudio.Analyzers
{
    /// <summary>
    /// Returns a list if target frameworks for given file path, by exporting all
    /// available IProjectTargetFrameworkProviders and looking for the one supporting
    /// given file's DTE project.
    /// </summary>
    internal interface IProjectTargetFrameworkProviderExporter
    {
        IEnumerable<string> GetTargetFrameworks(string filePath);
    }
}
