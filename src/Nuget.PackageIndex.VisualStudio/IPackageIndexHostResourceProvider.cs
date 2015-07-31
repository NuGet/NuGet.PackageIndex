// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Nuget.PackageIndex.VisualStudio
{
    /// <summary>
    /// Package index has very few strings visible to user (at this point only one)
    /// so we don't want to localize this assembly for one string and would just query 
    /// it from the host.
    /// </summary>
    public interface IPackageIndexHostResourceProvider
    {
        /// <summary>
        /// Returns localized resource string from host resources
        /// </summary>
        string GetResourceString(string resourceId);
    }
}
