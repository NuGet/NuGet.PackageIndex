using System;
using System.ComponentModel.Composition;
using System.Diagnostics;

namespace Nuget.PackageIndex.VisualStudio
{
    /// <summary>
    /// Should be exported by project systems that update local nuget packages,
    /// to refresh local package index
    /// </summary>
    [Export(typeof(ILocalNugetPackageIndex))]
    internal class LocalNugetPackageIndex : ILocalNugetPackageIndex
    {
        public void Synchronize()
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    // Fire and forget. When new package is installed, we are not just adding this package to the index,
                    // but instead we attempt full sync. Full sync in this case would only sync new packages that don't 
                    // exist in the index which should be fast. The reason for full sync is that when there are several 
                    // instances of VS and each tries to update index at the same tiime, only one would succeed, other 
                    // would notive that index is locked and skip this operation. Thus if all VS instances attempt full 
                    // sync at least one of them would do it and add all new packages to the index.
                    var indexFactory = new PackageIndexFactory();
                    var builder = indexFactory.GetLocalIndexBuilder();

                    builder.Build(newOnly: true);
                }
                catch(Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
            }).ConfigureAwait(false);
        }
    }
}
