// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.using System.Collections.Generic;

using System;
using System.Collections.Generic;
using System.Linq;
using Nuget.PackageIndex.Models;
using NuGet.Frameworks;
using System.Runtime.Versioning;
using NuGet.Versioning;

namespace Nuget.PackageIndex.Client
{
    /// <summary>
    /// Contains helper methods for operations with packages and target frameworks
    /// </summary>
    public static class TargetFrameworkHelper
    {
        private static NuGetFramework ParseFrameworkName(string framework)
        {
            return NuGetFramework.Parse(framework);
        }

        public static bool AreCompatible(TargetFrameworkMetadata projectFramework, IEnumerable<string> packageFrameworks)
        {
            var nugetPackageFrameworks = packageFrameworks.Select(x => ParseFrameworkName(x));
            return AreCompatible(projectFramework, nugetPackageFrameworks);
        }

        private static bool AreCompatible(TargetFrameworkMetadata projectFramework, IEnumerable<NuGetFramework> packageFrameworks)
        {
            var reducer = new FrameworkReducer();

            var nearest = reducer.GetNearest(ParseFrameworkName(projectFramework.TargetFrameworkShortName), packageFrameworks);
            if (nearest == null)
            {
                foreach (var import in projectFramework.Imports)
                {
                    nearest = reducer.GetNearest(ParseFrameworkName(import), packageFrameworks);
                    if (nearest != null)
                    {
                        break;
                    }
                }
            }
            return nearest != null;
        }

        public static FrameworkName GetFrameworkName(string framework)
        {
            var nugetFramework = ParseFrameworkName(framework);

            return new FrameworkName(nugetFramework.DotNetFrameworkName);
        }

        /// <summary>
        /// Returns all packages that support given list of target frameworks.
        /// Note: if projectTargetFrameworks is null or empty list it means that project type
        /// did not support discovery of target frameworks and thus we default to display all available
        /// packages for discoverability purpose (this whole feature is about discoverability). In this case
        /// we let user to figure out what he wants to do with unsupported packages, we at least show them.
        /// </summary>
        public static IEnumerable<IPackageIndexModelInfo> GetSupportedPackages(IEnumerable<IPackageIndexModelInfo> modelsWithGivenType, 
                                                                 IEnumerable<TargetFrameworkMetadata> projectTargetFrameworks,
                                                                 bool allowHigherVersions)
        {
            List<IPackageIndexModelInfo> supportedPackages;
            if (projectTargetFrameworks != null && projectTargetFrameworks.Any())
            {
                // if project target frameworks are provided, try to filter
                supportedPackages = new List<IPackageIndexModelInfo>();
                foreach (var model in modelsWithGivenType)
                {
                    if (SupportsProjectTargetFrameworks(model, projectTargetFrameworks)
                        && !PackageExistsInTheProject(model, projectTargetFrameworks, allowHigherVersions))
                    {
                        supportedPackages.Add(model);
                    }
                }
            }
            else
            {
                // if project did not provide target frameworks to us, show all packages with requested type
                supportedPackages = new List<IPackageIndexModelInfo>(modelsWithGivenType);
            }

            return supportedPackages;
        }

        /// <summary>
        /// Checks if package supports any of project's target frameworks
        /// </summary>
        public static bool SupportsProjectTargetFrameworks(IPackageIndexModelInfo typeInfo, IEnumerable<TargetFrameworkMetadata> projectTargetFrameworks)
        {
            // if we find at least any framework in package that current project supports,
            // we show this package to user.
            if (typeInfo.TargetFrameworks == null 
                || !typeInfo.TargetFrameworks.Any()
                || typeInfo.TargetFrameworks.Any(x => x.Equals("Unsupported", StringComparison.OrdinalIgnoreCase)))
            {
                // In this case:
                //      if package did not specify any target frameworks
                //         or one of the frameworks is new and Nuget.Core can not recognize it - returning Unsupported
                //      we follow our default behavior and display as much as possible to the user, return true to show the package
                return true;
            }
            else
            {
                var packageFrameworkNames = typeInfo.TargetFrameworks.Select(x => ParseFrameworkName(x)).ToList();
                foreach (var projectFramework in projectTargetFrameworks)
                {
                    if (packageFrameworkNames.Any(x => AreCompatible(projectFramework, new[] { x })))
                    {
                        // if at least any project target framework supports package - display it
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Returns a list of distinct target frameworks for the list of given projects
        /// </summary>
        public static IEnumerable<TargetFrameworkMetadata> GetDistinctTargetFrameworks(IEnumerable<ProjectMetadata> projects)
        {
            var distinctFrameworks = new Dictionary<string, TargetFrameworkMetadata>(StringComparer.OrdinalIgnoreCase);
            foreach (var project in projects)
            {
                if (project.TargetFrameworks == null)
                {
                    continue;
                }

                foreach (var fx in project.TargetFrameworks)
                {
                    TargetFrameworkMetadata tempFx;
                    if (!distinctFrameworks.TryGetValue(fx.TargetFrameworkShortName, out tempFx))
                    {
                        distinctFrameworks.Add(fx.TargetFrameworkShortName, fx);
                    }
                }
            }

            return distinctFrameworks.Values;
        }

        private static bool PackageExistsInTheProject(IPackageIndexModelInfo packageInfo, IEnumerable<TargetFrameworkMetadata> projectTargetFrameworks, bool allowHigherVersions)
        {
            if (allowHigherVersions)
            {
                var packageVersion = new NuGetVersion(packageInfo.PackageVersion);
                return projectTargetFrameworks.Any(x => x.Packages.Any(p =>
                        {
                            var projecsPackageVersion = new NuGetVersion(p.Value);
                            // if project has package with given name and version less than in index
                            return p.Key.Equals(packageInfo.PackageName) && projecsPackageVersion <= packageVersion;
                        }
                ));
            }
            else
            {
               return projectTargetFrameworks.Any(x => x.Packages.Any(p => p.Key.Equals(packageInfo.PackageName)));
            }
        }
    }
}
