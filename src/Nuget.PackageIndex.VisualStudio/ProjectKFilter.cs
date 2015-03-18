using System;
using System.Diagnostics;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Nuget.PackageIndex.Client.Analyzers;
using Nuget.PackageIndex.VisualStudio.CodeFixes;

namespace Nuget.PackageIndex.VisualStudio
{
    /// <summary>
    /// TODO remove this hack after RC
    /// </summary>
    internal class ProjectKFilter : IProjectFilter
    {

        public bool IsProjectSupported(string filePath)
        {
            return ThreadHelper.JoinableTaskFactory.Run(async delegate
            {
                // Switch to main thread
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                try
                {
                    var container = ServiceProvider.GlobalProvider.GetService<IComponentModel, SComponentModel>();
                    var project = DocumentExtensions.GetVsHierarchy(filePath, ServiceProvider.GlobalProvider).GetDTEProject();
                    if (project == null)
                    {
                        return false;
                    }

                    return project.FullName.ToLower().EndsWith(".xproj");
                }
                catch (Exception e)
                {
                    // we should not throw here, since it would create an exception that may be 
                    // visible to the user, instead just dump into debugger output or to package
                    // manager console.
                    // TODO Package manager console?
                    Debug.Write(string.Format("{0} \r\n {1}", e.Message, e.StackTrace));
                }
                return false;
            });            
        }
    }
}
