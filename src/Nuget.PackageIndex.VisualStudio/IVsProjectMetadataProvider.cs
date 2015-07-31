// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Nuget.PackageIndex.Client;

namespace Nuget.PackageIndex.VisualStudio
{
    /// <summary>
    /// A VS service interface that Dnx project system package implements and exposes.
    /// We need it since Roslyn currently does not provide information about projects for 
    /// files being analyzed and it also does not work with VS UI thread correctly. Thus
    /// the only way to obtain project info without going to DTE objects (UI thread) is
    /// to get this service from DNX package. Since we only support DNX projects currently,
    /// its ok. Later when we would need to support all C#/VB projects we would need 
    /// Roslyn changes anayway.
    /// </summary>
    [ComImport]
    [Guid("37F5C652-85DD-4E18-AC1E-12EB20BAD1EE")]
    public interface IVsProjectMetadataProvider
    {
        Task<IEnumerable<ProjectMetadata>> GetProjectsAsync(string filePath);
    }
}
