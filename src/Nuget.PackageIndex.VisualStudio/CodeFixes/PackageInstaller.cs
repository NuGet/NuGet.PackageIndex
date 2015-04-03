using System.Threading;
using Microsoft.CodeAnalysis;
using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Document = Microsoft.CodeAnalysis.Document;
using NuGet;
using NuGet.VisualStudio;
using TypeInfo = Nuget.PackageIndex.Models.TypeInfo;
using Nuget.PackageIndex.Client.CodeFixes;
using Task = System.Threading.Tasks.Task;

namespace Nuget.PackageIndex.VisualStudio.CodeFixes
{
    /// <summary>
    /// Implementation of IPackageInstaller that does actual package installation via 
    /// importing IVsPackageInstaller component and installing a package in a DTE project
    /// </summary>
    internal class PackageInstaller : IPackageInstaller, IDisposable
    {
        private bool _disposed = false;

        private readonly SVsServiceProvider _serviceProvider;

        /// <summary>
        /// Note: since analyzer and code fix providers are created only once per VS instance,
        /// we need to make sure that no project specific info is used in the constructor,
        /// since when user reopens solution this constructor will not be called again and
        /// thus objects from old projects might be used later when code fix is being applied.
        /// So export all project specific objects in InstallPackage everytime.
        /// </summary>
        /// <param name="serviceProvider"></param>
        public PackageInstaller(SVsServiceProvider serviceProvider)
        {
            Debug.Assert(serviceProvider != null);

            _serviceProvider = serviceProvider;
            var container = _serviceProvider.GetService<IComponentModel, SComponentModel>();

            IVsPackageInstallerEvents nugetInstallerEvents = container.DefaultExportProvider.GetExportedValue<IVsPackageInstallerEvents>();
            nugetInstallerEvents.PackageInstalled += OnPackageInstalled;
        }

        public void InstallPackage(Workspace workspace, Document document, TypeInfo typeInfo, CancellationToken cancellationToken = default(CancellationToken))
        {
            Debug.Assert(typeInfo != null);

            ThreadHelper.JoinableTaskFactory.RunAsync(async delegate {
                // Switch to main thread
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                try
                {
                    var container = _serviceProvider.GetService<IComponentModel, SComponentModel>();
                    var docHierarchy = document.GetVsHierarchy(_serviceProvider);
                    if (docHierarchy == null)
                    {
                        return;
                    }

                    var project = docHierarchy.GetDTEProject();
                    var projectSpecificInstallers = container.DefaultExportProvider.GetExportedValues<IProjectPackageInstaller>();
                    if (projectSpecificInstallers != null && projectSpecificInstallers.Any())
                    {
                        var supportedInstaller = projectSpecificInstallers.FirstOrDefault(x => x.SupportsProject(project));
                        if (supportedInstaller != null)
                        {
                            if (await SafeExecuteActionAsync(
                                delegate {
                                    var frameworks = typeInfo.TargetFrameworks == null 
                                                        ? null 
                                                        : typeInfo.TargetFrameworks.Select(x => VersionUtility.ParseFrameworkName(x)).ToList();
                                    return supportedInstaller.InstallPackageAsync(project, typeInfo.PackageName, typeInfo.PackageVersion, frameworks, cancellationToken);
                                }))
                            {
                                return; // package installed successfully
                            }
                        }
                    }

                    // if there no project specific installer use nuget default IVsPackageInstaller
                    var installer = container.DefaultExportProvider.GetExportedValue<IVsPackageInstaller>() as IVsPackageInstaller2;
                    var packageSourceProvider = container.DefaultExportProvider.GetExportedValue<IVsPackageSourceProvider>();
                    var sources = packageSourceProvider.GetSources(includeUnOfficial: true, includeDisabled: false).Select(x => x.Value).ToList();

                    // if pass all sources to InstallPackageAsync it would throw Central Directory corrupt
                    // if one of the feeds is not supported and stop installation - nuget's bug
                    foreach (var source in sources)
                    {
                        if (await SafeExecuteActionAsync(
                                delegate {
                                    return installer.InstallPackageAsync(project, new[] { source }, typeInfo.PackageName, typeInfo.PackageVersion, true, cancellationToken);
                                }))
                        {
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    // we should not throw here, since it would create an exception that may be 
                    // visible to the user, instead just dump into debugger output or to package
                    // manager console.
                    // TODO Package manager console?
                    Debug.Write(string.Format("{0} \r\n {1}", e.Message, e.StackTrace));
                }
            });
        }

        private async System.Threading.Tasks.Task<bool> SafeExecuteActionAsync(Func<Task> action)
        {
            bool success = true;
            try
            {
                await action.Invoke();
            }
            catch (Exception ex)
            {
                // InvalidOperationException would mean package not found or other install error, 
                // but for us actually any exception means success=false, so try next feed
                success = false;
                Debug.Write(string.Format("{0} \r\n {1}", ex.Message, ex.StackTrace));
            }

            return success;
        }

        private void OnPackageInstalled(IVsPackageMetadata metadata)
        {
            // Fire and forget. When new package is installed, we are not just adding this package to the index,
            // but instead we attempt full sync. Full sync in this case would only sync new packages that don't 
            // exist in the index which should be fast. The reason for full sync is that when there are several 
            // instances of VS and each tries to update index at the same tiime, only one would succeed, other 
            // would notive that index is locked and skip this operation. Thus if all VS instances attempt full 
            // sync at least one of them would do it and add all new packages to the index.
            var indexFactory = new PackageIndexFactory();
            var builder = indexFactory.GetLocalIndexBuilder();
            builder.BuildAsync(newOnly: true);
        }

        ~PackageInstaller()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    var container = _serviceProvider.GetService<IComponentModel, SComponentModel>();
                    IVsPackageInstallerEvents nugetInstallerEvents = container.DefaultExportProvider.GetExportedValue<IVsPackageInstallerEvents>();
                    nugetInstallerEvents.PackageInstalled -= OnPackageInstalled;
                }
                catch(Exception e)
                {
                    Debug.WriteLine(e.Message); // do nothing for now, log?                    
                }

                _disposed = true;
            }

            GC.SuppressFinalize(this);
        }
    }
}
