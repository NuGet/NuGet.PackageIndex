// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Nuget.PackageIndex.Client;

namespace Nuget.PackageIndex.VisualStudio.Editor
{
    // TODO Temporary
    //[Export(typeof(IViewTaggerProvider))]
    //[ContentType("code")]
    //[TagType(typeof(NugetPackageTag))]
    internal class NugetPackageTaggerProvider : IViewTaggerProvider
    {
        [Import]
        private IAddPackageAnalyzerFactory AnalyzerFactory { get; set; }

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag

        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            return new NugetPackageTagger(textView, AnalyzerFactory) as ITagger<T>;
        }
    }
}