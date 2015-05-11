// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Nuget.PackageIndex.Client.Analyzers;

namespace Nuget.PackageIndex.VisualStudio
{
    /// <summary>
    /// This filter is needed only until we enable Missing Package suggestions for all C# projects.
    /// For now we limit this feature only to ProjectK scenarios.
    /// </summary>
    internal class DnxProjectFilter : IProjectFilter, IDisposable
    {
        private ConcurrentDictionary<string, string> FilesCache = 
                        new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private bool _disposed;
        private SolutionEvents _solutionEvents;
        private object _cacheLock = new object();
        private bool _solutionIsClosing;

        public DnxProjectFilter()
        {
            var container = ServiceProvider.GlobalProvider.GetService<IComponentModel, SComponentModel>();

            ThreadHelper.JoinableTaskFactory.Run(async delegate
            {
                // Switch to main thread
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

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
            });
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
                    Debug.Write(e.ToString());
                }
            });

            if (!string.IsNullOrEmpty(projectFullName))
            {
                FilesCache.AddOrUpdate(filePath, (k) => projectFullName, (k, v) => projectFullName);
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

            // remove files that belong to the project that was removed/renamed
            projectFullPath = projectFullPath.ToLowerInvariant();
            foreach (var kvp in FilesCache.ToList())
            {
                // project paths in the cache arready in lower case
                if (kvp.Value.Equals(projectFullPath))
                {
                    string removedVal = string.Empty;
                    FilesCache.TryRemove(kvp.Key, out removedVal);
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
            Debug.Assert(project != null, "Project passed to OnProjectRemoved DTE event was null.");
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
                        _solutionEvents.BeforeClosing -= OnBeforeSolutionClosed;
                        _solutionEvents.Opened -= OnSolutionOpened;
                        _solutionEvents.ProjectRemoved -= OnProjectRemoved;
                        _solutionEvents.ProjectRenamed -= OnProjectRenamed;
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
