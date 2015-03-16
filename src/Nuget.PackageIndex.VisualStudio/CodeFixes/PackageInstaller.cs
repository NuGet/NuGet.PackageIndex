using System.Threading;
using Microsoft.CodeAnalysis;
using System;
using System.Diagnostics;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Document = Microsoft.CodeAnalysis.Document;
using NuGet.VisualStudio;
using Nuget.PackageIndex.Models;
using Nuget.PackageIndex.Client.CodeFixes;

namespace Nuget.PackageIndex.VisualStudio.CodeFixes
{
    /// <summary>
    /// Implementation of IPackageInstaller that does actual package installation via 
    /// importing IVsPackageInstaller component and installing a package in a DTE project
    /// </summary>
    internal class PackageInstaller : IPackageInstaller
    {
        private readonly SVsServiceProvider _serviceProvider;

        public PackageInstaller(SVsServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void InstallPackage(Workspace workspace, Document document, TypeModel typeModel, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThreadHelper.JoinableTaskFactory.Run(async delegate {
                // Switch to main thread
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                try
                {
                    var container = _serviceProvider.GetService<IComponentModel, SComponentModel>();
                    var installer = container.DefaultExportProvider.GetExportedValue<IVsPackageInstaller>();
                    var project = document.GetVsHierarchy(_serviceProvider).GetDTEProject();

                    installer.InstallPackage(null, project, typeModel.PackageName, typeModel.PackageVersion, true);
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
    }
}
