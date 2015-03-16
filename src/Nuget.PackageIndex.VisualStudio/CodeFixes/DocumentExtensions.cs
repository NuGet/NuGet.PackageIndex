using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;

namespace Nuget.PackageIndex.VisualStudio.CodeFixes
{
    internal static class DocumentExtensions
    {
        public static IVsHierarchy GetVsHierarchy(this Document document, SVsServiceProvider serviceProvider)
        {
            IVsHierarchy hier = null;

            var rdt = new RunningDocumentTable(serviceProvider);
            uint itemId = 0;
            uint docCookie = 0;
            rdt.FindDocument(document.FilePath, out hier, out itemId, out docCookie);

            return hier;
        }
    }
}
