﻿using System.Threading;
using Microsoft.CodeAnalysis;
using Nuget.PackageIndex.Models;

namespace Nuget.PackageIndex.Client.CodeFixes
{
    /// <summary>
    /// Abstraction that knows how to install a package to a given workspace and document.
    /// For example there could be several different IDEs that would provide their own 
    /// implementation of IPackageIndexInstaller
    /// </summary>
    public interface IPackageInstaller
    {
        void InstallPackage(Workspace workspace, Document document, TypeModel typeModel, CancellationToken cancellationToken = default(CancellationToken));
    }
}
