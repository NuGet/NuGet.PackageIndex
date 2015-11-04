// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.Versioning;

namespace Nuget.PackageIndex.NugetHelpers
{
    internal static class DotNetGenerationMapping
    {
        private static readonly Version _version45 = new Version(4, 5);
        private static readonly Dictionary<FrameworkName, Version> _generationMappings = new Dictionary<FrameworkName, Version>()
        {
            // dnxcore50
            { new FrameworkName(DnxVersionUtility.DnxCoreFrameworkIdentifier, new Version(5, 0)), new Version(5, 5) },

            // netcore50/uap10
            { new FrameworkName(DnxVersionUtility.NetCoreFrameworkIdentifier, new Version(5, 0)), new Version(5, 4) },
            { new FrameworkName(DnxVersionUtility.UapFrameworkIdentifier, new Version(10, 0)), new Version(5, 4) },

            // netN
            { new FrameworkName(DnxVersionUtility.NetFrameworkIdentifier, new Version(4, 5)), new Version(5, 2) },
            { new FrameworkName(DnxVersionUtility.NetFrameworkIdentifier, new Version(4, 5, 1)), new Version(5, 3) },
            { new FrameworkName(DnxVersionUtility.NetFrameworkIdentifier, new Version(4, 5, 2)), new Version(5, 3) },
            { new FrameworkName(DnxVersionUtility.NetFrameworkIdentifier, new Version(4, 6)), new Version(5, 4) },
            { new FrameworkName(DnxVersionUtility.NetFrameworkIdentifier, new Version(4, 6, 1)), new Version(5, 5) },

            // dnxN
            { new FrameworkName(DnxVersionUtility.DnxFrameworkIdentifier, new Version(4, 5)), new Version(5, 2) },
            { new FrameworkName(DnxVersionUtility.DnxFrameworkIdentifier, new Version(4, 5, 1)), new Version(5, 3) },
            { new FrameworkName(DnxVersionUtility.DnxFrameworkIdentifier, new Version(4, 5, 2)), new Version(5, 3) },
            { new FrameworkName(DnxVersionUtility.DnxFrameworkIdentifier, new Version(4, 6)), new Version(5, 4) },
            { new FrameworkName(DnxVersionUtility.DnxFrameworkIdentifier, new Version(4, 6, 1)), new Version(5, 5) },

            // winN
            { new FrameworkName(DnxVersionUtility.WindowsFrameworkIdentifier, new Version(8, 0)), new Version(5, 2) },
            { new FrameworkName(DnxVersionUtility.NetCoreFrameworkIdentifier, new Version(4, 5)), new Version(5, 2) },
            { new FrameworkName(DnxVersionUtility.WindowsFrameworkIdentifier, new Version(8, 1)), new Version(5, 3) },
            { new FrameworkName(DnxVersionUtility.NetCoreFrameworkIdentifier, new Version(4, 5, 1)), new Version(5, 3) },

            // windows phone silverlight
            { new FrameworkName(DnxVersionUtility.WindowsPhoneFrameworkIdentifier, new Version(8, 0)), new Version(5, 1) },
            { new FrameworkName(DnxVersionUtility.WindowsPhoneFrameworkIdentifier, new Version(8, 1)), new Version(5, 1) },
            { new FrameworkName(DnxVersionUtility.SilverlightFrameworkIdentifier, new Version(8, 0), DnxVersionUtility.WindowsPhoneFrameworkIdentifier), new Version(5, 1) },

            // wpaN
            { new FrameworkName(DnxVersionUtility.WindowsPhoneAppFrameworkIdentifier, new Version(8, 1)), new Version(5, 3) }
        };

        public static IEnumerable<FrameworkName> Expand(FrameworkName input)
        {
            // Try to convert the project framework into an equivalent target framework
            // If the identifiers didn't match, we need to see if this framework has an equivalent framework that DOES match.
            // If it does, we use that from here on.
            // Example:
            //  If the Project Targets DNX, Version=4.5.1. It can accept Packages targetting .NETFramework, Version=4.5.1
            //  so since the identifiers don't match, we need to "translate" the project target framework to .NETFramework
            //  however, we still want direct DNX == DNX matches, so we do this ONLY if the identifiers don't already match
            // This also handles .NET Generation mappings

            yield return input;

            var gen = GetGeneration(input);

            // dnxN -> netN -> dotnetY
            if (input.Identifier.Equals(DnxVersionUtility.DnxFrameworkIdentifier))
            {
                yield return new FrameworkName(DnxVersionUtility.NetFrameworkIdentifier, input.Version);
                if (gen != null)
                {
                    yield return gen;
                }
            }
            // uap10 -> netcore50 -> wpa81 -> dotnetY
            else if (input.Identifier.Equals(DnxVersionUtility.UapFrameworkIdentifier) && input.Version == NugetConstants.Version10_0)
            {
                yield return new FrameworkName(DnxVersionUtility.NetCoreFrameworkIdentifier, NugetConstants.Version50);
                yield return new FrameworkName(DnxVersionUtility.WindowsPhoneAppFrameworkIdentifier, new Version(8, 1));
                if (gen != null)
                {
                    yield return gen;
                }
            }
            // netcore50 (universal windows apps) -> wpa81 -> dotnetY
            else if (input.Identifier.Equals(DnxVersionUtility.NetCoreFrameworkIdentifier) && input.Version == NugetConstants.Version50)
            {
                yield return new FrameworkName(DnxVersionUtility.WindowsPhoneAppFrameworkIdentifier, new Version(8, 1));
                if (gen != null)
                {
                    yield return gen;
                }
            }
            // others just map to a generation (if any)
            else if (gen != null)
            {
                yield return gen;
            }
        }

        public static FrameworkName GetGeneration(FrameworkName input)
        {
            Version version;
            if (!_generationMappings.TryGetValue(input, out version))
            {
                return null;
            }
            return new FrameworkName(DnxVersionUtility.NetPlatformFrameworkIdentifier, version);
        }
    }
}
