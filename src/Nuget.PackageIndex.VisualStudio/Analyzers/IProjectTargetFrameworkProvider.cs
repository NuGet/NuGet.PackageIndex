// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System.Collections.Generic;
using Nuget.PackageIndex.Client;

namespace Nuget.PackageIndex.VisualStudio.Analyzers
{
    public delegate void ProjectTargetFrameworkChanged(object sender, string projectFullPath);

    /// <summary>
    /// Projects should Export this interface to provide target frameworks supported 
    /// by given DTE project. This is needed since different rpojec system types might have
    /// different strategy for target frameworks. For example csproj has one target framework, 
    /// xproj can have multiple.
    /// </summary>
    public interface IProjectTargetFrameworkProvider
    {
        bool SupportsProject(string projectFullPath);
        IEnumerable<TargetFrameworkMetadata> GetTargetFrameworks(EnvDTE.Project project);
        event ProjectTargetFrameworkChanged TargetFrameworkChanged;
        void RefreshTargetFrameworks(string projectFullPath);
    }
}
