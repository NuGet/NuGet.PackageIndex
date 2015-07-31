// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Nuget.PackageIndex.VisualStudio.Editor
{
    [Export(typeof(IGlyphFactoryProvider))]
    [Name("NugetPackageGlyph")]
    [Order(After = "VsTextMarker")]
    [ContentType("code")]
    [TagType(typeof(NugetPackageTag))]
    internal sealed class NugetPackageGlyphFactoryProvider : IGlyphFactoryProvider
    {
        public IGlyphFactory GetGlyphFactory(IWpfTextView view, IWpfTextViewMargin margin)
        {
            return new NugetPackageGlyphFactory();
        }
    }

    internal class NugetPackageGlyphFactory : IGlyphFactory
    {
        private const double GlyphSize = 16.0;

        public UIElement GenerateGlyph(IWpfTextViewLine line, IGlyphTag tag)
        {
            if (tag == null || !(tag is NugetPackageTag))
            {
                return null;
            }

            var glyphIcon = new Image();
            glyphIcon.Width = GlyphSize;
            glyphIcon.Height = GlyphSize;
            glyphIcon.Source = ImageHelper.GetImageSource(KnownMonikers.NuGet);

            return glyphIcon;
        }
    }

    internal class NugetPackageTag : IGlyphTag
    {

    }
}
