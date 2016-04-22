// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Nuget.PackageIndex.Client.CodeFixes;
using Nuget.PackageIndex.Client;
using Nuget.PackageIndex.Models;
using Document = Microsoft.CodeAnalysis.Document;
using Task = System.Threading.Tasks.Task;

namespace Nuget.PackageIndex.VisualStudio.CodeFixes
{
    /// <summary>
    /// Implementation of IPackageInstaller that does actual package installation via 
    /// importing IVsPackageInstaller component and installing a package in a DTE project
    /// </summary>
    internal class PackageInstaller : IPackageInstaller
    {
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
        }

        public void InstallPackage(Workspace workspace, 
                                   Document document,
                                   IPackageIndexModelInfo packageInfo, 
                                   IEnumerable<ProjectMetadata> projects, 
                                   CancellationToken cancellationToken = default(CancellationToken))
        {
            Debug.Assert(packageInfo != null);

            ThreadHelper.JoinableTaskFactory.RunAsync(async delegate {
                foreach (var project in projects)
                {
                    try
                    {
                        var container = _serviceProvider.GetService<IComponentModel, SComponentModel>();
                        var projectSpecificInstallers = container.DefaultExportProvider.GetExportedValues<IProjectPackageInstaller>();
                        if (projectSpecificInstallers != null && projectSpecificInstallers.Any())
                        {
                            var supportedInstaller = projectSpecificInstallers.FirstOrDefault(x => x.SupportsProject(project.ProjectPath));
                            if (supportedInstaller != null)
                            {
                                if (await SafeExecuteActionAsync(
                                    delegate 
                                    {
                                        var frameworksToInstall = new List<FrameworkName>();
                                        foreach (var projectFrameworkMetadata in project.TargetFrameworks)
                                        {
                                            if (TargetFrameworkHelper.AreCompatible(projectFrameworkMetadata, packageInfo.TargetFrameworks))
                                            {
                                                frameworksToInstall.Add(TargetFrameworkHelper.GetFrameworkName(projectFrameworkMetadata.TargetFrameworkShortName));
                                            }
                                        }

                                        return supportedInstaller.InstallPackageAsync(project.ProjectPath, 
                                                                                      packageInfo.PackageName, 
                                                                                      packageInfo.PackageVersion, 
                                                                                      frameworksToInstall, 
                                                                                      cancellationToken);
                                    }))
                                {
                                    continue; // package installed successfully
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        // we should not throw here, since it would create an exception that may be 
                        // visible to the user, instead just dump into debugger output or to package
                        // manager console.
                        // TODO Package manager console?
                        Debug.Write(e.ToString());
                    }
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
                Debug.Write(ex.ToString());
            }

            return success;
        }
    }
}
