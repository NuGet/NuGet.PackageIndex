using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Nuget.PackageIndex.Client;

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
        /// <summary>
        /// Stores file and it's corresponding unique project name
        /// </summary>
        private Dictionary<string, string> FilesCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, IEnumerable<TargetFrameworkMetadata>> ProjectFrameworksCache = new Dictionary<string, IEnumerable<TargetFrameworkMetadata>>(StringComparer.OrdinalIgnoreCase);
        private bool _disposed;
        private SolutionEvents _solutionEvents;

        [ImportingConstructor]
        public ProjectTargetFrameworkProviderExporter([Import]SVsServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;

            var container = ServiceProvider.GetService<IComponentModel, SComponentModel>();
            Providers = container.DefaultExportProvider.GetExportedValues<IProjectTargetFrameworkProvider>();

            ThreadHelper.JoinableTaskFactory.Run(async delegate
            {
                // Switch to main thread
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                // Get the DTE
                var dte = ServiceProvider.GetService(typeof(DTE)) as DTE;
                Debug.Assert(dte != null, "Couldn't get the DTE. Crash incoming.");

                var events = (Events2)dte.Events;
                if (events != null)
                {
                    _solutionEvents = events.SolutionEvents;
                    Debug.Assert(_solutionEvents != null, "Cannot get SolutionEvents");

                    // clear all cache if solution closed.
                    _solutionEvents.AfterClosing += OnAfterSolutionClosing;
                    _solutionEvents.ProjectRemoved += OnProjectRemoved;
                }
            });

            foreach (var provider in Providers)
            {
                provider.TargetFrameworkChanged += OnProjectTargetFrameworkChanged;
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
        public IEnumerable<TargetFrameworkMetadata> GetTargetFrameworks(string filePath)
        {
            IEnumerable<TargetFrameworkMetadata> resultFrameworks = null;
            string projectFullPath = null;

            // try to get framework info for a given file from cache
            lock (_cacheLock)
            {
                if (FilesCache.TryGetValue(filePath, out projectFullPath)
                    && ProjectFrameworksCache.TryGetValue(projectFullPath, out resultFrameworks))
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

                    var provider = Providers.FirstOrDefault(x => x.SupportsProject(dteProject.FullName));
                    if (provider != null)
                    {
                        projectFullPath = dteProject.FullName;
                        resultFrameworks = provider.GetTargetFrameworks(dteProject);
                    }
                }
                catch (Exception e)
                {
                    // Add to Package Manager console?
                    Debug.Write(e.ToString());
                }
            });

            // add file and project frameworks to cache
            lock (_cacheLock)
            {
                if (ProjectFrameworksCache.Keys.Contains(projectFullPath))
                {
                    ProjectFrameworksCache[projectFullPath] = resultFrameworks;
                }
                else
                {
                    ProjectFrameworksCache.Add(projectFullPath, resultFrameworks);
                }

                if (FilesCache.Keys.Contains(filePath))
                {
                    FilesCache[filePath] = projectFullPath;
                }
                else
                {
                    FilesCache.Add(filePath, projectFullPath);
                }
            }

            return resultFrameworks;
        }

        private void OnProjectTargetFrameworkChanged(object sender, string projectFullPath)
        {
            lock (_cacheLock)
            {
                // here we need to remove from cache files that belong to a given project
                if (ProjectFrameworksCache.Keys.Contains(projectFullPath))
                {
                    ProjectFrameworksCache.Remove(projectFullPath);
                }

                var filesToRemove = FilesCache.Where(x => x.Value.Equals(projectFullPath, StringComparison.OrdinalIgnoreCase))
                                              .Select(x => x.Key)
                                              .ToList();
                foreach (var file in filesToRemove)
                {
                    FilesCache.Remove(file);
                }
            }
        }

        private void ClearCache()
        {
            lock (_cacheLock)
            {
                ProjectFrameworksCache.Clear();
                FilesCache.Clear();
            }
        }

        private void OnAfterSolutionClosing()
        {
            ClearCache();
        }

        private void OnProjectRemoved(Project project)
        {
            if (project == null)
            {
                return;
            }

            OnProjectTargetFrameworkChanged(this, project.FullName);
        }

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                try
                {
                    if (_solutionEvents != null)
                    {
                        _solutionEvents.AfterClosing -= OnAfterSolutionClosing;
                        _solutionEvents.ProjectRemoved -= OnProjectRemoved;
                    }

                    if (Providers != null)
                    {
                        foreach (var provider in Providers)
                        {
                            provider.TargetFrameworkChanged -= OnProjectTargetFrameworkChanged;
                        }
                    }

                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.ToString()); // do nothing for now, log?                    
                }

                _disposed = true;
            }
        }

        #endregion
    }
}
