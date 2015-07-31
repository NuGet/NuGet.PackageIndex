// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;

namespace Nuget.PackageIndex.VisualStudio
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
