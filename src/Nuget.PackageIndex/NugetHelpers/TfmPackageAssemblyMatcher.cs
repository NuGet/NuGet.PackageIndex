// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using NuGet;

namespace Nuget.PackageIndex.NugetHelpers
{
    internal class TfmPackageAssemblyMatcher
    {
        public static IEnumerable<string> GetAssembliesForFramework(FrameworkName framework, IPackage package, IEnumerable<string> packageAssemblies)
        {
            var patterns = PatternDefinitions.DotNetPatterns;

            if (packageAssemblies == null)
            {
                return null;
            }

            var files = packageAssemblies.Select(p => p.Replace(Path.DirectorySeparatorChar, '/'));
            var contentItems = new ContentItemCollection();
            contentItems.Load(files);

            var criteriaBuilderWithTfm = new SelectionCriteriaBuilder(patterns.Properties.Definitions);

            criteriaBuilderWithTfm = criteriaBuilderWithTfm
                 .Add["tfm", framework];

            var criteria = criteriaBuilderWithTfm.Criteria;

            var allReferencesGroup = contentItems.FindBestItemGroup(criteria, patterns.CompileTimeAssemblies, patterns.ManagedAssemblies);
            List<string> allReferencesGroupAssemblies = null;
            if (allReferencesGroup != null)
            {
                allReferencesGroupAssemblies = allReferencesGroup.Items.Select(t => t.Path).ToList();
            }

            if (allReferencesGroupAssemblies == null)
            {
                return null;
            }

            IEnumerable<string> oldLibGroupAssemblies = null;
            var oldLibGroup = contentItems.FindBestItemGroup(criteria, patterns.ManagedAssemblies);
            if (oldLibGroup != null)
            {
                oldLibGroupAssemblies = oldLibGroup.Items.Select(p => p.Path).ToList();
            }

            // COMPAT: Support lib/contract so older packages can be consumed
            string contractPath = "lib/contract/" + package.Id + ".dll";
            var hasContract = files.Any(path => contractPath.Equals(path, StringComparison.OrdinalIgnoreCase));
            var hasLib = oldLibGroupAssemblies != null && oldLibGroupAssemblies.Any();

            if (hasContract && hasLib && !DnxVersionUtility.IsDesktop(framework))
            {
                allReferencesGroupAssemblies.Clear();
                allReferencesGroupAssemblies.Add(contractPath);
            }

            // See if there's a list of specific references defined for this target framework
            IEnumerable<PackageReferenceSet> referenceSets;
            if (DnxVersionUtility.GetNearest(framework, package.PackageAssemblyReferences, out referenceSets))
            {
                // Get the first compatible reference set
                var referenceSet = referenceSets.FirstOrDefault();

                if (referenceSet != null)
                {
                    // Remove all assemblies of which names do not appear in the References list
                    allReferencesGroupAssemblies.RemoveAll(path => path.StartsWith("lib/") 
                                                                   && !referenceSet.References.Contains(Path.GetFileName(path), StringComparer.OrdinalIgnoreCase));
                }
            }

            return allReferencesGroupAssemblies.Select(p => p.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
