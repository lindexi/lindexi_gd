/**
/// ScrimpNet.Core Library
/// Copyright © 2005-2011
///
/// This module is Copyright © 2005-2011 Steve Powell
/// All rights reserved.
///
/// This library is free software; you can redistribute it and/or
/// modify it under the terms of the Microsoft Public License (Ms-PL)
/// 
/// This library is distributed in the hope that it will be
/// useful, but WITHOUT ANY WARRANTY; without even the implied
/// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
/// PURPOSE.  See theMicrosoft Public License (Ms-PL) License for more
/// details.
///
/// You should have received a copy of the Microsoft Public License (Ms-PL)
/// License along with this library; if not you may 
/// find it here: http://www.opensource.org/licenses/ms-pl.html
///
/// Steve Powell, spowell@scrimpnet.com
**/
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using ScrimpNet.IO;

namespace ScrimpNet.Drawing
{
    /// <summary>
    /// Miscellaneous utitlities that handle common image tasks
    /// </summary>
    public class DrawingUtils
    {
        /// <summary>
        /// Convert stream that is pointing to a underlaying image into an instance of an image.  Does not return stream reader to starting position
        /// </summary>
        /// <param name="inputStream">Read stream points to start of image</param>
        /// <returns>Converted image</returns>
        public static Image ConvertImageStreamToImage(Stream inputStream)
        {
            return Image.FromStream(inputStream, true, true);
        }

        /// <summary>
        /// Convert an image to a byte array
        /// </summary>
        /// <param name="image">Hydrated image to convert</param>
        /// <returns>Byte representation of an hydrated image object</returns>
        public static byte[] ImageToByteArray(Image image)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                image.Save(ms, ImageFormat.Jpeg);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Create an image from an array of bytes that were originally an image
        /// </summary>
        /// <param name="imageBytes">Bytes to convert</param>
        /// <returns>Created image</returns>
        public static Image ByteArrayToImage(byte[] imageBytes)
        {
            return Image.FromStream(IOUtils.StreamFromBytes(imageBytes));
        }

        /// <summary>
        ///  Method will calculate the proportionate size based on current size and desired maximum size
        /// </summary>
        /// <param name="currentSize"></param>
        /// <param name="maxSize"></param>
        /// <returns>The size the new thumbnail keeping original proportion but not exceeding maximum size</returns>
        private static Size calculateSize(Size currentSize, Size maxSize)
        {
            double widthAdjustment = (double)maxSize.Width / (double)currentSize.Width;
            double heightAdjustment = (double)maxSize.Height / (double)currentSize.Height;
            double reductionAmount = (widthAdjustment < heightAdjustment) ? widthAdjustment : heightAdjustment;
            int newWidth = (int)Math.Floor(currentSize.Width * reductionAmount);
            int newHeight = (int)Math.Floor(currentSize.Height * reductionAmount);
            Size calculatedSize = new Size(newWidth, newHeight);
            return calculatedSize;
        }

        /// <summary>
        /// Create a new image that is proportionally sized to <paramref name="maxSize"/>
        /// </summary>
        /// <param name="sourceImage">Hydrated image that will be converted</param>
        /// <param name="maxSize">Image will not exceed these dimensions</param>
        /// <returns>Created image</returns>
        public static Image ThumbnailCreate(Image sourceImage, Size maxSize)
        {

            Image.GetThumbnailImageAbort myCallback = new Image.GetThumbnailImageAbort(thumbnailCallback);
            Size calculatedSize = calculateSize(sourceImage.Size, maxSize);
            Image thumb = sourceImage.GetThumbnailImage(calculatedSize.Width, calculatedSize.Height, myCallback, System.IntPtr.Zero);
            return thumb;
        }

        /// <summary>
        /// Create a new image that is proportionally sized to <paramref name="maxSize"/>
        /// </summary>
        /// <param name="imageBytes">Array of bytes that were at one time an image</param>
        /// <param name="maxSize">Image will not exceed these dimensions</param>
        /// <returns>Created image</returns>
        public static Image ThumbnailCreate(byte[] imageBytes, Size maxSize)
        {
            return ThumbnailCreate(ByteArrayToImage(imageBytes), maxSize);
        }

        private static bool thumbnailCallback() //stub.  Used for GetThumbnailImage function only
        {
            return true;
        }
    }
}
