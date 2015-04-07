using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using EnvDTE;
using EnvDTE80;
using Nuget.PackageIndex.Client.Analyzers;

namespace Nuget.PackageIndex.VisualStudio
{
    /// <summary>
    /// This filter is needed only until we enable Missing Package suggestions for all C# projects.
    /// For now we limit this feature only to ProjectK scenarios.
    /// </summary>
    internal class ProjectKFilter : IProjectFilter
    {
        private Dictionary<string, string> FilesCache = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        private bool _disposed;
        private SolutionEvents _solutionEvents;
        private object _cacheLock = new object();
        private bool _solutionIsClosing;

        public ProjectKFilter()
        {
            var container = ServiceProvider.GlobalProvider.GetService<IComponentModel, SComponentModel>();

            // Get the DTE
            var dte = ServiceProvider.GlobalProvider.GetService(typeof(DTE)) as DTE;
            Debug.Assert(dte != null, "Couldn't get the DTE. Crash incoming.");

            var events = (Events2)dte.Events;
            if (events != null)
            {
                _solutionEvents = events.SolutionEvents;
                Debug.Assert(_solutionEvents != null, "Cannot get SolutionEvents");

                // clear all cache if solution closed.
                _solutionEvents.BeforeClosing += OnBeforeSolutionClosed;
                _solutionEvents.Opened += OnSolutionOpened;
                _solutionEvents.ProjectRemoved += OnProjectRemoved;
                _solutionEvents.ProjectRenamed += OnProjectRenamed;
            }
        }

        public bool IsProjectSupported(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || _solutionIsClosing)
            {
                return false;
            }

            string projectFullName;
            if (FilesCache.TryGetValue(filePath, out projectFullName))
            {
                return IsProjectFileSupported(projectFullName);
            }

            ThreadHelper.JoinableTaskFactory.Run(async delegate
            {
                // Switch to main thread
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                try
                {
                    var container = ServiceProvider.GlobalProvider.GetService<IComponentModel, SComponentModel>();
                    var hierarchy = DocumentExtensions.GetVsHierarchy(filePath, ServiceProvider.GlobalProvider);
                    if (hierarchy == null)
                    {
                        return;
                    }

                    var project = hierarchy.GetDTEProject();
                    if (project == null || string.IsNullOrEmpty(project.FullName))
                    {
                        return;
                    }

                    projectFullName = project.FullName.ToLowerInvariant();
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

            if (!string.IsNullOrEmpty(projectFullName))
            {
                lock (_cacheLock)
                {
                    if (FilesCache.Keys.Contains(filePath))
                    {
                        FilesCache[filePath] = projectFullName;
                    }
                    else
                    {
                        FilesCache.Add(filePath, projectFullName);
                    }
                }
            }

            return IsProjectFileSupported(projectFullName);
        }

        private bool IsProjectFileSupported(string projectFile)
        {
            return !string.IsNullOrEmpty(projectFile) && projectFile.EndsWith(".xproj");
        }

        private void ClearCache(string projectFullPath)
        {
            if (string.IsNullOrEmpty(projectFullPath))
            {
                return;
            }

            lock(_cacheLock)
            {
                // remove files that belong to the project that was removed/renamed
                projectFullPath = projectFullPath.ToLowerInvariant();
                foreach (var kvp in FilesCache.ToList())
                {
                    // project paths in the cache arready in lower case
                    if (kvp.Value.Equals(projectFullPath))
                    {
                        FilesCache.Remove(kvp.Key);
                    }
                }
            }
        }

        private void OnBeforeSolutionClosed()
        {
            _solutionIsClosing = true;
        }

        private void OnSolutionOpened()
        {
            _solutionIsClosing = false;
        }

        private void OnProjectRemoved(Project project)
        {
            if (project == null)
            {
                return;
            }

            var projectFullPath = string.Empty;
            ThreadHelper.JoinableTaskFactory.Run(async delegate
            {
                // Switch to main thread
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                projectFullPath = project.FullName;
            });

            ClearCache(projectFullPath);
        }

        private void OnProjectRenamed(Project project, string oldName)
        {
            ClearCache(oldName);
        }

        ~ProjectKFilter()
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
                        _solutionEvents.BeforeClosing -= OnBeforeSolutionClosed;
                        _solutionEvents.Opened -= OnSolutionOpened;
                        _solutionEvents.ProjectRemoved -= OnProjectRemoved;
                        _solutionEvents.ProjectRenamed -= OnProjectRenamed;
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
