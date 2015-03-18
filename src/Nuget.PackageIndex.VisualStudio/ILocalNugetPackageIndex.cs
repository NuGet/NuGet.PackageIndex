using System;
using System.Runtime.InteropServices;

namespace Nuget.PackageIndex.VisualStudio
{
    /// <summary>
    /// Should be exported by project systems that update local nuget packages,
    /// to refresh local package index
    /// </summary>
    [ComImport, Guid("5CC0C383-2D75-4CB6-A30A-7B78164222F2")]
    public interface ILocalNugetPackageIndex
    {
        void Synchronize();
    }
}
