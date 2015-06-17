// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System.Collections.Generic;

namespace Nuget.PackageIndex.NugetHelpers
{
    internal class PatternDefinitions
    {
        public static readonly PatternDefinitions DotNetPatterns = new PatternDefinitions();

        public PropertyDefinitions Properties { get; }

        public ContentPatternDefinition CompileTimeAssemblies { get; }
        public ContentPatternDefinition ManagedAssemblies { get; }
        public ContentPatternDefinition ResourceAssemblies { get; }
        public ContentPatternDefinition NativeLibraries { get; }

        public PatternDefinitions()
        {
            Properties = new PropertyDefinitions();

            ManagedAssemblies = new ContentPatternDefinition
            {
                GroupPatterns =
                    {
                        "runtimes/{rid}/lib/{tfm}/{any?}",
                        "lib/{tfm}/{any?}"
                    },
                PathPatterns =
                    {
                        "runtimes/{rid}/lib/{tfm}/{assembly}",
                        "lib/{tfm}/{assembly}"
                    },
                PropertyDefinitions = Properties.Definitions
            };

            ManagedAssemblies.GroupPatterns.Add(new PatternDefinition
            {
                Pattern = "lib/{assembly?}",
                Defaults = new Dictionary<string, object>
                    {
                        {  "tfm", DnxVersionUtility.ParseFrameworkName("net") }
                    }
            });

            ManagedAssemblies.PathPatterns.Add(new PatternDefinition
            {
                Pattern = "lib/{assembly}",
                Defaults = new Dictionary<string, object>
                    {
                        {  "tfm", DnxVersionUtility.ParseFrameworkName("net") }
                    }
            });

            CompileTimeAssemblies = new ContentPatternDefinition
            {
                GroupPatterns =
                    {
                        "ref/{tfm}/{any?}",
                    },
                PathPatterns =
                    {
                        "ref/{tfm}/{assembly}",
                    },
                PropertyDefinitions = Properties.Definitions,
            };

            ResourceAssemblies = new ContentPatternDefinition
            {
                GroupPatterns =
                    {
                        "runtimes/{rid}/lib/{tfm}/{locale?}/{any?}",
                        "lib/{tfm}/{locale?}/{any?}"
                    },
                PathPatterns =
                    {
                        "runtimes/{rid}/lib/{tfm}/{locale}/{resources}",
                        "lib/{tfm}/{locale}/{resources}"
                    },
                PropertyDefinitions = Properties.Definitions
            };

            ResourceAssemblies.GroupPatterns.Add(new PatternDefinition
            {
                Pattern = "lib/{locale}/{resources?}",
                Defaults = new Dictionary<string, object>
                    {
                        {  "tfm", DnxVersionUtility.ParseFrameworkName("net") }
                    }
            });

            ResourceAssemblies.PathPatterns.Add(new PatternDefinition
            {
                Pattern = "lib/{locale}/{resources}",
                Defaults = new Dictionary<string, object>
                    {
                        {  "tfm", DnxVersionUtility.ParseFrameworkName("net") }
                    }
            });

            NativeLibraries = new ContentPatternDefinition
            {
                GroupPatterns =
                    {
                        "runtimes/{rid}/native/{any?}",
                        "native/{any?}",
                    },
                PathPatterns =
                    {
                        "runtimes/{rid}/native/{any}",
                        "native/{any}",
                    },
                PropertyDefinitions = Properties.Definitions,
            };
        }
    }
}
