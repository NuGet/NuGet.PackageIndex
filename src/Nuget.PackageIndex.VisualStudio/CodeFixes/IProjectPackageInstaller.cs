using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace Nuget.PackageIndex.VisualStudio.CodeFixes
{
    /// <summary>
    /// Provides a way for project systems to supply a way to install packages before we used IVsPackageInstaller
    /// </summary>
    public interface IProjectPackageInstaller
    {
        bool SupportsProject(EnvDTE.Project project);
        Task InstallPackageAsync(EnvDTE.Project project, string packageName, string packageVersion, IEnumerable<FrameworkName> frameworks, CancellationToken cancellationToken);
    }
}
