namespace Nuget.PackageIndex.Client.Analyzers
{
    /// <summary>
    /// Note: this is a temporary hack to limit package index analysis only to Project K projects
    /// for RC, since other teams would need to test and signoff on this fetaure in their projects
    ///
    /// A filter that excludes non xproj projects from analysis
    /// </summary>
    public interface IProjectFilter
    {
        bool IsProjectSupported(string filePath);
    }
}
