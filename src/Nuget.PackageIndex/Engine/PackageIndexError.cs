using System;
using Nuget.PackageIndex.Models;

namespace Nuget.PackageIndex.Engine
{
    /// <summary>
    /// If an indexing operation over given entry (add/remove) returned error, entry and the actual 
    /// Exception would be wraped by this class and returned to the caller.
    /// </summary>
    public class PackageIndexError
    {
        public IPackageIndexModel Entry { get; set; }
        public Exception Exception { get; set; }

        public PackageIndexError(IPackageIndexModel entry, Exception exception)
        {
            Entry = entry;
            Exception = exception;
        }
    }
}
