// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: Helper methods for code that uses types from System.Drawing.
//

using System;
using System.IO;
using System.IO.Pipes;
using System.Security;

namespace MS.Internal
{
    internal static class SystemDrawingHelper
    {
        // return true if the data is a bitmap
        internal static bool IsBitmap(object data)
        {
            return false;
        }

        // return true if the data is an Image
        internal static bool IsImage(object data)
        {
            return false;
        }

        // return true if the data is a graphics metafile
        internal static bool IsMetafile(object data)
        {
            return false;
        }

        // return the handle from a metafile
        internal static IntPtr GetHandleFromMetafile(Object data)
        {
            return IntPtr.Zero;
        }

        // Get the metafile from the handle of the enhanced metafile.
        internal static Object GetMetafileFromHemf(IntPtr hMetafile)
        {
            return false;
        }

        // Get a bitmap from the given data (either BitmapSource or Bitmap)
        internal static object GetBitmap(object data)
        {
            return false;
        }

        // Get a bitmap handle from the given data (either BitmapSource or Bitmap)
        // Also return its width and height.
        internal static IntPtr GetHBitmap(object data, out int width, out int height)
        {
            width = height = 0;
            return IntPtr.Zero;
        }

        // Get a bitmap handle from a Bitmap
        internal static IntPtr GetHBitmapFromBitmap(object data)
        {
            return IntPtr.Zero;
        }

        // Convert a metafile to HBitmap
        internal static IntPtr ConvertMetafileToHBitmap(IntPtr handle)
        {
            return IntPtr.Zero;
        }

        // return a stream for the ExifUserComment in the given Gif
        internal static Stream GetCommentFromGifStream(Stream stream)
        {
            return new MemoryStream();
        }

        // write a metafile stream to the output stream in PNG format
        internal static void SaveMetafileToImageStream(MemoryStream metafileStream, Stream imageStream)
        {
        }

        //returns bitmap snapshot of selected area
        //this code takes a BitmapImage and converts it to a Bitmap so it can be put on the clipboard
        internal static object GetBitmapFromBitmapSource(object source)
        {
            return false;
        }
    }
}
