// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace Nuget.PackageIndex.VisualStudio.CodeFixes
{
    /// <summary>
    /// Provides a way for project systems to supply a way to install packages before we used IVsPackageInstaller
    /// </summary>
    public interface IProjectPackageInstaller
    {
        bool SupportsProject(string projectPath);
        Task InstallPackageAsync(string projectPath, string packageName, string packageVersion, 
                                 IEnumerable<FrameworkName> frameworks, CancellationToken cancellationToken);
    }
}
