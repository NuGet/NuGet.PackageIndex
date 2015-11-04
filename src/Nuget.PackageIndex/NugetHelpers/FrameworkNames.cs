// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Nuget.PackageIndex.NugetHelpers
{
    internal static class FrameworkNames
    {
        public static class ShortNames
        {
            public const string Dnx = "dnx";
            public const string DnxCore = "dnxcore";
            public const string Dnx451 = Dnx + "451";
            public const string Dnx46 = Dnx + "46";
            public const string DnxCore50 = DnxCore + "50";
            public const string Dnx452 = Dnx + "452";
        }

        public static class LongNames
        {
            private const string VersionPrefix = ", Version=v";
            public const string Dnx = "DNX";
            public const string DnxCore = "DNXCore";
            public const string NetFramework = ".NETFramework";
            public const string Dnx451 = Dnx + VersionPrefix + "4.5.1";
            public const string Dnx46 = Dnx + VersionPrefix + "4.6";
            public const string DnxCore50 = DnxCore + VersionPrefix + "5.0";
        }
    }
}
