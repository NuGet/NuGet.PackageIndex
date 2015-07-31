// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;

namespace Nuget.PackageIndex
{
    /// <summary>
    /// Is responsible for initialization of the local index. If there no local index,
    /// it would schedule a task that will create an index on user machine.
    /// </summary>
    public interface IPackageIndexFactory
    {
        ILocalPackageIndex GetLocalIndex(bool createIfNotExists);
        IRemotePackageIndex GetRemoteIndex();
        ILocalPackageIndexBuilder GetLocalIndexBuilder(bool createIfNotExists);
        void DetachFromLocalIndex();
        CancellationToken GetCancellationToken();
    }
}
