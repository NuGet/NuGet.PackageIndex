using System.Collections.Generic;
using Nuget.PackageIndex.Client;

namespace Nuget.PackageIndex.VisualStudio.Analyzers
{
    /// <summary>
    /// Returns a list if target frameworks for given file path, by exporting all
    /// available IProjectTargetFrameworkProviders and looking for the one supporting
    /// given file's DTE project.
    /// </summary>
    internal interface IProjectTargetFrameworkProviderExporter
    {
        IEnumerable<TargetFrameworkMetadata> GetTargetFrameworks(string filePath);
    }
}
