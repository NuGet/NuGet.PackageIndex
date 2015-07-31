// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using System.Collections.Generic;
using Nuget.PackageIndex.Models;

namespace Nuget.PackageIndex.Client.CodeFixes
{
    /// <summary>
    /// Operation that does actual package intallation. 
    /// Note Ad package code fix has several operations: add import statements and add package.
    /// </summary>
    public class AddPackageOperation : CodeActionOperation
    {
        private readonly IPackageInstaller _packageInstaller;
        private readonly Document _document;
        private readonly IPackageIndexModelInfo _packageInfo;
        private readonly IEnumerable<ProjectMetadata> _projects;
        private readonly string _title;

        public AddPackageOperation(IPackageInstaller packageInstaller, 
                                   Document document,
                                   IPackageIndexModelInfo packageInfo, 
                                   IEnumerable<ProjectMetadata> projects,
                                   string title)
        {
            _packageInstaller = packageInstaller;
            _document = document;
            _packageInfo = packageInfo;
            _projects = projects;
            _title = title;
        }

        public override void Apply(Workspace workspace, CancellationToken cancellationToken = default(CancellationToken))
        {
            _packageInstaller.InstallPackage(workspace, _document, _packageInfo, _projects, cancellationToken);
        }

        public override string Title
        {
            get
            {
                return _title;
            }
        }
    }
}
