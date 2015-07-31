// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using NuGet;

namespace Nuget.PackageIndex.NugetHelpers
{
    public class PropertyDefinitions
    {
        public PropertyDefinitions()
        {
            Definitions = new Dictionary<string, ContentPropertyDefinition>
                {
                    { "language", _language },
                    { "tfm", _targetFramework },
                    { "rid", _rid },
                    { "assembly", _assembly },
                    { "dynamicLibrary", _dynamicLibrary },
                    { "resources", _resources },
                    { "locale", _locale },
                    { "any", _any },
                };
        }

        public IDictionary<string, ContentPropertyDefinition> Definitions { get; }

        ContentPropertyDefinition _language = new ContentPropertyDefinition
        {
            Table =
                {
                    { "cs", "CSharp" },
                    { "vb", "Visual Basic" },
                    { "fs", "FSharp" },
                }
        };

        ContentPropertyDefinition _targetFramework = new ContentPropertyDefinition
        {
            Table =
                {
                    { "any", new FrameworkName(DnxVersionUtility.NetPlatformFrameworkIdentifier, new Version(5, 0)) }
                },
            Parser = TargetFrameworkName_Parser,
            OnIsCriteriaSatisfied = TargetFrameworkName_IsCriteriaSatisfied,
            OnCompare = TargetFrameworkName_NearestCompareTest
        };

        ContentPropertyDefinition _rid = new ContentPropertyDefinition
        {
            Parser = name => name
        };

        ContentPropertyDefinition _assembly = new ContentPropertyDefinition
        {
            FileExtensions = { ".dll", ".exe", ".winmd" }
        };

        ContentPropertyDefinition _dynamicLibrary = new ContentPropertyDefinition
        {
            FileExtensions = { ".dll", ".dylib", ".so" }
        };

        ContentPropertyDefinition _resources = new ContentPropertyDefinition
        {
            FileExtensions = { ".resources.dll" }
        };

        ContentPropertyDefinition _locale = new ContentPropertyDefinition
        {
            Parser = Locale_Parser,
        };

        ContentPropertyDefinition _any = new ContentPropertyDefinition
        {
            Parser = name => name
        };


        internal static object Locale_Parser(string name)
        {
            if (name.Length == 2)
            {
                return name;
            }
            else if (name.Length >= 4 && name[2] == '-')
            {
                return name;
            }

            return null;
        }

        internal static object TargetFrameworkName_Parser(string name)
        {
            var result = DnxVersionUtility.ParseFrameworkName(name);

            if (result != DnxVersionUtility.UnsupportedFrameworkName)
            {
                return result;
            }

            return new FrameworkName(name, new Version(0, 0));
        }

        internal static bool TargetFrameworkName_IsCriteriaSatisfied(object criteria, object available)
        {
            var criteriaFrameworkName = criteria as FrameworkName;
            var availableFrameworkName = available as FrameworkName;

            if (criteriaFrameworkName != null && availableFrameworkName != null)
            {
                return DnxVersionUtility.IsCompatible(criteriaFrameworkName, availableFrameworkName);
            }

            return false;
        }

        private static int TargetFrameworkName_NearestCompareTest(object projectFramework, object criteria, object available)
        {
            var projectFrameworkName = projectFramework as FrameworkName;
            var criteriaFrameworkName = criteria as FrameworkName;
            var availableFrameworkName = available as FrameworkName;

            if (criteriaFrameworkName != null
                && availableFrameworkName != null
                && projectFrameworkName != null)
            {
                var frameworks = new[] { criteriaFrameworkName, availableFrameworkName };

                // Find the nearest compatible framework to the project framework.
                var nearest = DnxVersionUtility.GetNearest(projectFrameworkName, frameworks);

                if (nearest == null)
                {
                    return -1;
                }

                if (criteriaFrameworkName.Equals(nearest))
                {
                    return -1;
                }

                if (availableFrameworkName.Equals(nearest))
                {
                    return 1;
                }
            }

            return 0;
        }

        private class GetNearestHelper : IFrameworkTargetable
        {
            public FrameworkName Framework { get; }

            public IEnumerable<FrameworkName> SupportedFrameworks
            {
                get
                {
                    yield return Framework;
                }
            }

            public GetNearestHelper(FrameworkName framework) { Framework = framework; }


        }
    }
}
