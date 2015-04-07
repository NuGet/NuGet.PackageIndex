using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Nuget.PackageIndex.VisualStudio
{
    /// <summary>
    /// Should be exported by project systems that update local nuget packages,
    /// to refresh local package index
    /// </summary>
    [Export(typeof(ILocalNugetPackageIndex))]
    internal class LocalNugetPackageIndex : ILocalNugetPackageIndex
    {
        private Task _indexBuildTask;

        public void Synchronize()
        {
            try
            {
                if (_indexBuildTask == null || _indexBuildTask.IsCompleted)
                {
                    RebuildIndex();
                }
                else
                {
                    // if there is index build task already running, remember only last 
                    // synchronize request and continue after current rebuild is complete.
                    // This is to avoid resync 10 times when there multiple synchronize 
                    // requests come and we don't need actually to rebuild index for them 
                    // all, but just for the first and maybe the last one
                    _indexBuildTask.ContinueWith(t => RebuildIndex());
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        private void RebuildIndex()
        {
            // Fire and forget. When new package is installed, we are not just adding this package to the index,
            // but instead we attempt full sync. Full sync in this case would only sync new packages that don't 
            // exist in the index which should be fast. The reason for full sync is that when there are several 
            // instances of VS and each tries to update index at the same tiime, only one would succeed, other 
            // would notive that index is locked and skip this operation. Thus if all VS instances attempt full 
            // sync at least one of them would do it and add all new packages to the index.
            var indexFactory = new PackageIndexFactory();
            var builder = indexFactory.GetLocalIndexBuilder();
            _indexBuildTask = builder.BuildAsync(newOnly: builder.Index.IndexExists);
        }
    }
}
