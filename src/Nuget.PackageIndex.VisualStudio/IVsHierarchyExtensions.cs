// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace Nuget.PackageIndex.VisualStudio
{
    internal static class IVsHierarchyExtensions
    {
        public static EnvDTE.Project GetDTEProject(this IVsHierarchy hierarchy)
        {
            UIThreadHelper.VerifyOnUIThread();
            object extObject;
            if (ErrorHandler.Succeeded(hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out extObject)))
            {
                return extObject as EnvDTE.Project;
            }
            return null;
        }
    }
}
