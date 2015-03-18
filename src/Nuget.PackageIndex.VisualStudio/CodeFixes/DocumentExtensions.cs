using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using System;

namespace Nuget.PackageIndex.VisualStudio.CodeFixes
{
    internal static class DocumentExtensions
    {
        public static IVsHierarchy GetVsHierarchy(this Document document, IServiceProvider serviceProvider)
        {
            return GetVsHierarchy(document.FilePath, serviceProvider);
        }

        public static IVsHierarchy GetVsHierarchy(string filePath, IServiceProvider serviceProvider)
        {
            IVsHierarchy hier = null;

            var rdt = new RunningDocumentTable(serviceProvider);
            uint itemId = 0;
            uint docCookie = 0;
            rdt.FindDocument(filePath, out hier, out itemId, out docCookie);

            return hier;
        }
    }
}
