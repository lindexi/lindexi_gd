// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//#define OLD_ISF

using MS.Utility;
using System;
using System.Security;
using System.Windows;
//using System.Windows.Media.Imaging;
using System.IO;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Ink;
using MS.Internal.IO.Packaging;
using WpfInk.PresentationCore.System.Windows;

namespace MS.Internal.Ink.InkSerializedFormat
{
    internal class StrokeCollectionSerializer
    {
        #region Constants (Static Fields)
        internal static readonly double AvalonToHimetricMultiplier = 2540.0d / 96.0d;
        internal static readonly double HimetricToAvalonMultiplier = 96.0d / 2540.0d;
        internal static readonly TransformDescriptor IdentityTransformDescriptor;

        static StrokeCollectionSerializer()
        {
            TransformDescriptor transformDescriptor = new TransformDescriptor();
            transformDescriptor.Transform[0] = 1.0f;
            transformDescriptor.Tag = KnownTagCache.KnownTagIndex.TransformIsotropicScale;
            transformDescriptor.Size = 1;
            StrokeCollectionSerializer.IdentityTransformDescriptor = transformDescriptor;
        }
        #endregion

        #region Constructors

        // disable default constructor
        private StrokeCollectionSerializer() { }

        #endregion

        #region Public Fields

        internal PersistenceFormat CurrentPersistenceFormat = PersistenceFormat.InkSerializedFormat;
        internal CompressionMode CurrentCompressionMode = CompressionMode.Compressed;
        internal System.Collections.Generic.List<int> StrokeIds = null;
        #endregion

        #region Decoding

        #region Public Methods

        #endregion

        #region Private Methods

        /// <summary>
        /// ReliableRead
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="buffer"></param>
        /// <param name="requestedCount"></param>
        /// <returns></returns>
        internal static uint ReliableRead(Stream stream, byte[] buffer, uint requestedCount)
        {
            if (stream == null ||
                buffer == null ||
                requestedCount > buffer.Length)
            {
                throw new ArgumentException(StrokeCollectionSerializer.ISFDebugMessage("Invalid argument passed to ReliableRead"));
            }

            // let's read the whole block into our buffer
            uint totalBytesRead = 0;
            while (totalBytesRead < requestedCount)
            {
                int bytesRead = stream.Read(buffer,
                                (int)totalBytesRead,
                                (int)(requestedCount - totalBytesRead));
                if (bytesRead == 0)
                {
                    break;
                }
                totalBytesRead += (uint)bytesRead;
            }
            return totalBytesRead;
        }

        #endregion

        #endregion // Decoding

        #region Encoding

        #region Public Methods


        #endregion

        #region Private Methods



        #endregion // Private Methods

        internal class StrokeLookupEntry
        {
            internal uint MetricDescriptorTableIndex = 0;
            internal uint StrokeDescriptorTableIndex = 0;
            internal uint TransformTableIndex = 0;
            internal uint DrawingAttributesTableIndex = 0;

            // Compression algorithm data
            internal byte CompressionData = 0;

            internal int[][] ISFReadyStrokeData = null;
            internal bool StorePressure = false;
        }

        #endregion // Encoding

        #region Debugging Methods

        #endregion

        // [System.Diagnostics.Conditional("DEBUG_ISF")]
        internal static string ISFDebugMessage(string debugMessage)
        {
#if DEBUG
            return debugMessage;
#else
            return SR.Get(SRID.IsfOperationFailed);
#endif
        }

        #region Private Fields

        StrokeCollection _coreStrokes;
        private System.Collections.Generic.List<StrokeDescriptor> _strokeDescriptorTable = null;
        private System.Collections.Generic.List<TransformDescriptor> _transformTable = null;
        private System.Collections.Generic.List<DrawingAttributes> _drawingAttributesTable = null;
        private System.Collections.Generic.List<MetricBlock> _metricTable = null;
        private Vector _himetricSize = new Vector(0.0f, 0.0f);


            // The ink space rectangle (e.g. bounding box for GIF) is stored
            //      with the serialization info so that load/save roundtrip the
            //      rectangle
        private Rect _inkSpaceRectangle = new Rect();

        System.Collections.Generic.Dictionary<Stroke, StrokeLookupEntry> _strokeLookupTable = null;

        #endregion
    }
}

