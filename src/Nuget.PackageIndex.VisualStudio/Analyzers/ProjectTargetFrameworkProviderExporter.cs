using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;

namespace Nuget.PackageIndex.VisualStudio.Analyzers
{
    /// <summary>
    /// Imports all available IProjectTargetFrameworkProviders and tries to find target frameworks
    /// list for given file (it's DTE project), if any provider supports given project. If there
    /// no target framework providers found or not providers that support given project, we return 
    /// null which would mean "display all found packages" (since by default we want to reveal as
    /// many types and packages as possible and user can choose if he needs them)
    /// </summary>
    [Export(typeof(IProjectTargetFrameworkProviderExporter))]
    internal class ProjectTargetFrameworkProviderExporter : IProjectTargetFrameworkProviderExporter, IDisposable
    {        
        private IEnumerable<IProjectTargetFrameworkProvider> Providers { get; set; }

        private SVsServiceProvider ServiceProvider { get; set; }

        private object _cacheLock = new object();
        private Dictionary<string, IEnumerable<string>> FrameworkCache = new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase);
        private bool _disposed;
        private SolutionEvents _solutionEvents;

        [ImportingConstructor]
        public ProjectTargetFrameworkProviderExporter([Import]SVsServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;

            var container = ServiceProvider.GetService<IComponentModel, SComponentModel>();
            Providers = container.DefaultExportProvider.GetExportedValues<IProjectTargetFrameworkProvider>();

            // Get the DTE
            var dte = ServiceProvider.GetService(typeof(DTE)) as DTE;
            Debug.Assert(dte != null, "Couldn't get the DTE. Crash incoming.");

            var events = (Events2)dte.Events;
            if (events != null)
            {
                _solutionEvents = events.SolutionEvents;
                Debug.Assert(events != null, "Cannot get SolutionEvents");

                // clear all cache if solution closed.
                _solutionEvents.AfterClosing += OnAfterSolutionClosing;
            }
        }

        /// <summary>
        /// TODO We need to have some events from providers to invalidate the cache if some project's 
        /// target frameworks are changed (in this case we just need to remove/update cache items that have 
        /// same DTE as changed project).
       ///  For now cache will be invalidated when solution is closed and reopened.
        /// </summary>
        /// <param name="filePath">Path to a code file being analyzed</param>
        /// <returns></returns>
        public IEnumerable<string> GetTargetFrameworks(string filePath)
        {
            IEnumerable<string> resultFrameworks = null;

            // try to get framework info for a given file from cache
            lock(_cacheLock)
            {
                if (FrameworkCache.TryGetValue(filePath, out resultFrameworks))
                {
                    return resultFrameworks;
                }
            }

            if (Providers == null || !Providers.Any())
            {
                return null;
            }

            ThreadHelper.JoinableTaskFactory.Run(async delegate
            {
                // Switch to main thread
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                try
                {
                    var container = ServiceProvider.GetService<IComponentModel, SComponentModel>();
                    var dteProject = DocumentExtensions.GetVsHierarchy(filePath, ServiceProvider).GetDTEProject();

                    if (dteProject == null)
                    {
                        return;
                    }

                    var provider = Providers.FirstOrDefault(x => x.SupportsProject(dteProject));
                    if (provider != null)
                    {
                        resultFrameworks = provider.GetTargetFrameworks(dteProject);
                    }
                }
                catch (Exception e)
                {
                    // Add to Package Manager console?
                    Debug.Write(string.Format("{0}. Stack trace: {1}", e.Message, e.StackTrace));
                }
            });

            // add file to cache
            lock(_cacheLock)
            {
                FrameworkCache.Add(filePath, resultFrameworks);
            }

            return resultFrameworks;
        }

        private void ClearCache()
        {
            lock(_cacheLock)
            {
                FrameworkCache.Clear();
            }
        }

        private void OnAfterSolutionClosing()
        {
            ClearCache();
        }

        ~ProjectTargetFrameworkProviderExporter()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    if (_solutionEvents != null)
                    {
                        _solutionEvents.AfterClosing -= OnAfterSolutionClosing;
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message); // do nothing for now, log?                    
                }

                _disposed = true;
            }

            GC.SuppressFinalize(this);
        }
    }
}
