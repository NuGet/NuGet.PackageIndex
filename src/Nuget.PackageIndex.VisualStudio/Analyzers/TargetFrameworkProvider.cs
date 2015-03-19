using System;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.ComponentModelHost;
using Nuget.PackageIndex.Client.Analyzers;

namespace Nuget.PackageIndex.VisualStudio.Analyzers
{
    /// <summary>
    /// Implementation of ITargetFrameworkProvider which is passed to base analyzer
    /// class in Nuget.PackageIndex assembly which knows nothing about VS, thus we needed 
    /// to  abstract it out.
    /// </summary>
    public class TargetFrameworkProvider : ITargetFrameworkProvider
    {
        private IProjectTargetFrameworkProviderExporter _providersExporter;

        public IEnumerable<string> GetTargetFrameworks(string filePath)
        {
            try
            {
                if (_providersExporter == null)
                {
                    var container = ServiceProvider.GlobalProvider.GetService<IComponentModel, SComponentModel>();
                    _providersExporter = container.DefaultExportProvider.GetExportedValue<IProjectTargetFrameworkProviderExporter>();
                }

                return _providersExporter.GetTargetFrameworks(filePath);
            }
            catch (Exception e)
            {
                // by default show all packages if there is any exception - return null
                Debug.Write(e.Message);
            }
            return null;
        }
    }
}
