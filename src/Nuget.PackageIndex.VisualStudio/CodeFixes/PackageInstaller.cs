using System.Threading;
using Microsoft.CodeAnalysis;
using System;
using System.Diagnostics;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Document = Microsoft.CodeAnalysis.Document;
using NuGet.VisualStudio;
using TypeInfo = Nuget.PackageIndex.Models.TypeInfo;
using Nuget.PackageIndex.Client.CodeFixes;

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

        public PackageInstaller(SVsServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            var container = _serviceProvider.GetService<IComponentModel, SComponentModel>();
            IVsPackageInstallerEvents nugetInstallerEvents = container.DefaultExportProvider.GetExportedValue<IVsPackageInstallerEvents>();

            nugetInstallerEvents.PackageInstalled += OnPackageInstalled;
        }

        public void InstallPackage(Workspace workspace, Document document, TypeInfo typeInfo, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThreadHelper.JoinableTaskFactory.Run(async delegate {
                // Switch to main thread
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                try
                {
                    var container = _serviceProvider.GetService<IComponentModel, SComponentModel>();
                    var installer = container.DefaultExportProvider.GetExportedValue<IVsPackageInstaller>();
                    var project = document.GetVsHierarchy(_serviceProvider).GetDTEProject();

                    installer.InstallPackage(null, project, typeInfo.PackageName, typeInfo.PackageVersion, true);
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

        private void OnPackageInstalled(IVsPackageMetadata metadata)
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                // Fire and forget. When new package is installed, we are not just adding this package to the index,
                // but instead we attempt full sync. Full sync in this case would only sync new packages that don't 
                // exist in the index which should be fast. The reason for full sync is that when there are several 
                // instances of VS and each tries to update index at the same tiime, only one would succeed, other 
                // would notive that index is locked and skip this operation. Thus if all VS instances attempt full 
                // sync at least one of them would do it and add all new packages to the index.
                var indexFactory = new PackageIndexFactory();
                var builder = indexFactory.GetLocalIndexBuilder();
                
                var result = builder.Build(newOnly:true);
            }).ConfigureAwait(false);
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
