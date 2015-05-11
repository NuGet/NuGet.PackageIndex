// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using TypeInfo = Nuget.PackageIndex.Models.TypeInfo;

namespace Nuget.PackageIndex.Client.CodeFixes
{
    /// <summary>
    /// Code action that registers all operations necessary for package installation
    /// </summary>
    internal class AddPackageCodeAction : CodeAction
    {
        private readonly IPackageInstaller _packageInstaller;
        private readonly TypeInfo _typeInfo;
        private readonly Func<CancellationToken, Task<Document>> _createChangedDocument;
        private readonly string _titleFormat;
        private string _title;

        public AddPackageCodeAction(IPackageInstaller packageInstaller, 
                                    TypeInfo typeInfo, 
                                    string titleFormat, 
                                    Func<CancellationToken, Task<Document>> createChangedDocument)
        {
            _packageInstaller = packageInstaller;
            _typeInfo = typeInfo;
            _createChangedDocument = createChangedDocument;
            _titleFormat = titleFormat;
        }

        /// <summary>
        /// This title will be displayed in the Ctrl . menu
        /// </summary>
        public override string Title
        {
            get
            {
                if (_title == null)
                {
                    _title = string.Format(_titleFormat, string.Format("{0} {1}", _typeInfo.PackageName, _typeInfo.PackageVersion));
                }

                return _title; 
            }
        }

        /// <summary>
        /// Use title as Id since it will be always unique (package name + version)
        /// </summary>
#if RC
        public override string Id
#else
        public override string EquivalenceKey
#endif
        {
            get
            {
                return Title;
            }
        }

        /// <summary>
        /// Getting a changed document from provided Func
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
        {
            return _createChangedDocument(cancellationToken);
        }

        /// <summary>
        /// Main code fix action is happening here, we return list of operations to be executed by 
        /// code fix:
        ///     - AddPackageOperation does add package to the project,
        ///     - ApplyChangesOperation is a standard operation that stores changes in a document
        /// </summary>
        protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(CancellationToken cancellationToken)
        {
            var changedDocument = await GetChangedDocumentAsync(cancellationToken).ConfigureAwait(false);

            return new CodeActionOperation[]
            {
                new ApplyChangesOperation(changedDocument.Project.Solution), // add namespace
                new AddPackageOperation(_packageInstaller, changedDocument, _typeInfo, Title) // add package
            };
        }
    }
}
