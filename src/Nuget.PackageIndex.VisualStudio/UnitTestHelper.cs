// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Nuget.PackageIndex.VisualStudio
{
    /// <summary>
    /// Used to skip VS UI interaction when running unit tests
    /// </summary>
    internal static class UnitTestHelper
    {
        public static bool IsRunningUnitTests { get; set; }
    }
}
