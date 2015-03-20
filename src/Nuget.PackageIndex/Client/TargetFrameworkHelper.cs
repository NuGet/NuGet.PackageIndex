using System.Collections.Generic;
using System.Linq;
using NuGet;
using TypeInfo = Nuget.PackageIndex.Models.TypeInfo;

namespace Nuget.PackageIndex.Client
{
    /// <summary>
    /// Code imported form Microsoft.CodeAnalysis since it is internal there and we need it
    /// </summary>
    internal static class TargetFrameworkHelper
    {
        public static IEnumerable<TypeInfo> GetSupportedPackages(IEnumerable<TypeInfo> packagesWithGivenType, IEnumerable<string> projectTargetFrameworks)
        {
            // Check if project supports packages' target frameworks.
            // Note: if projectTargetFrameworks is null or empty list it means that project type
            // did not support discovery of target frameworks and thus we default to display all available
            // packages for discoverability purpose (this whole feature is about discoverability). In this case
            // we let user to figure out what he wants to do with unsupported packages, we at least show them.

            List<TypeInfo> supportedPackages;
            if (projectTargetFrameworks != null && projectTargetFrameworks.Any())
            {
                // if project target frameworks are provided, try to filter
                supportedPackages = new List<TypeInfo>();
                foreach (var packageInfo in packagesWithGivenType)
                {
                    if (SupportsProjectTargetFrameworks(packageInfo, projectTargetFrameworks))
                    {
                        supportedPackages.Add(packageInfo);
                    }
                }
            }
            else
            {
                // if project did not provide target frameworks to us, show all packages with requested type
                supportedPackages = new List<TypeInfo>(packagesWithGivenType);
            }

            return supportedPackages;
        }

        private static bool SupportsProjectTargetFrameworks(TypeInfo typeInfo, IEnumerable<string> projectTargetFrameworks)
        {
            // if we find at least any framework in package that current project supports,
            // we show this package to user.
            if (typeInfo.TargetFrameworks == null || !typeInfo.TargetFrameworks.Any())
            {
                // In this case package did not specify any target frameworks and we follow our default 
                // behavior and display as much as possible to the user
                return true;
            }
            else
            {
                var packageFrameworkNames = typeInfo.TargetFrameworks.Select(x => VersionUtility.ParseFrameworkName(x)).ToList();
                foreach (var projectFramework in projectTargetFrameworks)
                {
                    var projectFrameworkName = VersionUtility.ParseFrameworkName(projectFramework);
                    if (VersionUtility.IsCompatible(projectFrameworkName, packageFrameworkNames))
                    {
                        // if at least any project target framework supports package - display it
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
