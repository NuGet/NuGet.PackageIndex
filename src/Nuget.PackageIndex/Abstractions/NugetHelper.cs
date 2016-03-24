// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using NuGet.Packaging;
using NuGet.Repositories;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Nuget.PackageIndex.Abstractions
{
    public class NugetHelper : INugetHelper
    {
        public IEnumerable<string> GetPackageFiles(LocalPackageInfo package)
        {
            IList<string> files = null;
            using (var packageReader = new PackageArchiveReader(package.ZipPath))
            {
                if (Path.DirectorySeparatorChar != '/')
                {
                    files = packageReader
                        .GetFiles()
                        .Select(p => p.Replace(Path.DirectorySeparatorChar, '/'))
                        .ToList();
                }
                else
                {
                    files = packageReader
                        .GetFiles()
                        .ToList();
                }
            }

            return files;
        }

        public Stream GetStream(LocalPackageInfo package, string path)
        {
            var result = new MemoryStream();
            using (var packageReader = new PackageArchiveReader(package.ZipPath))
            {
                using (var stream = packageReader.GetStream(path))
                {
                    stream.CopyTo(result);
                    return result;
                }
            }
        }
    }
}
