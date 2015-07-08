// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using Nuget.PackageIndex.Client;

namespace Nuget.PackageIndex.VisualStudio
{
    /// <summary>
    /// CSharp Language specific helper that can determine if syntax node is one of supported types
    /// </summary>
    [Export(typeof(IAddPackageAnalyzerFactory))]
    public class AddPackageAnalyzerFactory : IAddPackageAnalyzerFactory
    {
        private Dictionary<string, IAddPackageAnalyzer> _analyzers = new Dictionary<string, IAddPackageAnalyzer>(StringComparer.OrdinalIgnoreCase);

        public IAddPackageAnalyzer GetAnalyzer(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return null;
            }

            var extension = Path.GetExtension(filePath);
            if (string.IsNullOrEmpty(extension))
            {
                return null;
            }

            IAddPackageAnalyzer analyzer = null;
            if (_analyzers.TryGetValue(extension, out analyzer))
            {
                return analyzer;
            }

            if (extension.Equals(".cs", StringComparison.OrdinalIgnoreCase))
            {
                analyzer = new AddPackageAnalyzer(new CSharpSyntaxHelper());
                _analyzers.Add(extension, analyzer);
            }

            return analyzer;
        }
    }
}
