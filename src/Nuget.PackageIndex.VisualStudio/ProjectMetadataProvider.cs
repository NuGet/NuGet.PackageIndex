// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.Shell;
using Nuget.PackageIndex.Client;

namespace Nuget.PackageIndex.VisualStudio
{
    /// <summary>
    /// Retrieves information from a service implemented in ProjectSystem
    /// </summary>
    internal sealed class ProjectMetadataProvider : IProjectMetadataProvider
    {
        private static IProjectMetadataProvider _instance;
        public static IProjectMetadataProvider Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ProjectMetadataProvider();
                }

                return _instance;
            }
        }

        private object _lock = new object();
        private IVsProjectMetadataProvider _vsProjectMetadataProvider;

        public IEnumerable<ProjectMetadata> GetProjects(string filePath)
        {
            try
            {
                lock (_lock)
                {
                    if (_vsProjectMetadataProvider == null)
                    {
                        _vsProjectMetadataProvider = ServiceProvider.GlobalProvider.GetService<IVsProjectMetadataProvider, IVsProjectMetadataProvider>();

                        if (_vsProjectMetadataProvider == null)
                        {
                            return null;
                        }
                    }

                    return ThreadHelper.JoinableTaskFactory.Run(async delegate
                    {
                        return await _vsProjectMetadataProvider.GetProjectsAsync(filePath);
                    });
                }
            }
            catch (Exception e)
            {
                Debug.Write(e.ToString());
            }

            return null;
        }
    }
}
