using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using TypeInfo = Nuget.PackageIndex.Models.TypeInfo;

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
        private readonly TypeInfo _typeInfo;
        private readonly string _title;

        public AddPackageOperation(IPackageInstaller packageInstaller, Document document, TypeInfo typeInfo, string title)
        {
            _packageInstaller = packageInstaller;
            _document = document;
            _typeInfo = typeInfo;
            _title = title;
        }

        public override void Apply(Workspace workspace, CancellationToken cancellationToken = default(CancellationToken))
        {
            _packageInstaller.InstallPackage(workspace, _document, _typeInfo, cancellationToken);
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
