// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Runtime.InteropServices;

namespace Nuget.PackageIndex.VisualStudio
{
    /// <summary>
    /// Should be exported by project systems that updates local nuget packages
    /// </summary>
    [ComImport, Guid("5CC0C383-2D75-4CB6-A30A-7B78164222F2")]
    public interface ILocalNugetPackageIndex
    {
        /// <summary>
        /// Initializes local index. If index does not exists starts index build process,
        /// if index exists but needs to be cleaned and rebuilt - cleans and rebuilds it.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Starts index synchronization with local packages
        /// </summary>
        void Synchronize();

        /// <summary>
        /// Stops all local inidex operations, should be called when VS instance is shutting down only.
        /// </summary>
        void Detach();
    }
}
