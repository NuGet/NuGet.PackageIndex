// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Nuget.PackageIndex.Client
{
    /// <summary>
    /// Provides information for each project containing given file:
    ///     - project full path
    ///     - list of project's target frameworks and their corresponding installed packages
    /// Note: This interface is an abstraction used in core logic, concrete implementations 
    /// for  different IDE are expected.
    /// </summary>
    public interface IProjectMetadataProvider
    {
        IEnumerable<ProjectMetadata> GetProjects(string filePath);
    }
}
