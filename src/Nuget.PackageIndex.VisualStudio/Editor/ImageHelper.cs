// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Nuget.PackageIndex.VisualStudio.Editor
{
    using System;
    using System.Drawing;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.VisualStudio.Imaging.Interop;
    using Microsoft.VisualStudio.PlatformUI;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using UIUtilities = Microsoft.Internal.VisualStudio.PlatformUI.Utilities;

    /// <summary>
    /// Helper methods for managing images and icons
    /// </summary>
    internal sealed class ImageHelper
    {
        private const int DefaultWidth = 16;
        private const int DefaultHeight = 16;

        private static IVsImageService2 imageService;

        private static ImageAttributes m_attributes = new ImageAttributes
        {
            StructSize = Marshal.SizeOf(typeof(ImageAttributes)),
            ImageType = (uint)_UIImageType.IT_Bitmap,
            Format = (uint)_UIDataFormat.DF_WinForms,
            Flags = (uint)_ImageAttributesFlags.IAF_RequiredFlags | unchecked((uint)_ImageAttributesFlags.IAF_Background),
        };

        private static IVsImageService2 ImageService
        {
            get
            {
                if (imageService == null)
                {
                    imageService = (IVsImageService2)Package.GetGlobalService(typeof(SVsImageService));
                }
                return imageService;
            }
        }

        /// <summary>
        /// Get the image based on moniker using the default dimensions
        /// </summary>
        public static Bitmap GetImage(ImageMoniker moniker)
        {
            System.Drawing.Color backColor = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey);
            return GetImage(moniker, backColor, DefaultWidth, DefaultHeight, true);
        }

        /// <summary>
        /// Get the image based on the moniker, backColor and dimensions
        /// </summary>
        public static Bitmap GetImage(ImageMoniker moniker, System.Drawing.Color backColor, int height, int width, bool scalingRequired)
        {
            m_attributes.Background = (uint)backColor.ToArgb();
            m_attributes.LogicalHeight = height;
            m_attributes.LogicalWidth = width;

            if (scalingRequired)
            {
                m_attributes.LogicalHeight = (int)(height * DpiHelper.LogicalToDeviceUnitsScalingFactorY);
                m_attributes.LogicalWidth = (int)(width * DpiHelper.LogicalToDeviceUnitsScalingFactorX);
            }

            IVsUIObject uIObjOK = ImageService.GetImage(moniker, m_attributes);
            Bitmap bitmap = (Bitmap)UIUtilities.GetObjectData(uIObjOK);
            bitmap.MakeTransparent(System.Drawing.Color.Magenta);

            return bitmap;
        }

        public static ImageSource GetImageSource(string imageName)
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(imageName);
            var image = Image.FromStream(stream);

            return GetImageSource(new Bitmap(image));
        }

        public static ImageSource GetImageSource(ImageMoniker imageMoniker)
        {
            return GetImageSource(GetImage(imageMoniker));
        }

        public static ImageSource GetImageSource(Bitmap bitmap)
        {
            IntPtr bmpPt = bitmap.GetHbitmap();
            BitmapSource bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                                           bmpPt,
                                           IntPtr.Zero,
                                           Int32Rect.Empty,
                                           BitmapSizeOptions.FromEmptyOptions());

            //freeze bitmapSource and clear memory to avoid memory leaks
            bitmapSource.Freeze();
            DeleteObject(bmpPt);

            return bitmapSource;
        }

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteObject(IntPtr value);
    }
}