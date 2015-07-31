// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Nuget.PackageIndex.Logging
{
    public interface ILog
    {
        LogLevel Level { get; set; }
        void WriteVerbose(string format, params object[] args);
        void WriteInformation(string format, params object[] args);
        void WriteError(string format, params object[] args);
    }
}
