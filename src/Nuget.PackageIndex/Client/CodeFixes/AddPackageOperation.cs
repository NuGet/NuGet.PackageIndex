using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
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
        private readonly TypeModel _typeModel;
        private readonly string _title;

        public AddPackageOperation(IPackageInstaller packageInstaller, Document document, TypeModel typeModel, string title)
        {
            _packageInstaller = packageInstaller;
            _document = document;
            _typeModel = typeModel;
            _title = title;
        }

        public override void Apply(Workspace workspace, CancellationToken cancellationToken = default(CancellationToken))
        {
            _packageInstaller.InstallPackage(workspace, _document, _typeModel, cancellationToken);
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
