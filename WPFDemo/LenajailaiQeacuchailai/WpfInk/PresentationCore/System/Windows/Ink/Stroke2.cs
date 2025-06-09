// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//#define DEBUG_RENDERING_FEEDBACK

using MS.Utility;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MS.Internal;
using MS.Internal.Ink;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;
using MS.Internal.PresentationCore;
using MS.Internal.YourAssemblyName;
using WpfInk.PresentationCore.System.Windows;

// Primary root namespace for TabletPC/Ink/Handwriting/Recognition in .NET

namespace System.Windows.Ink
{
    /// <summary>
    /// The hit-testing API of Stroke
    /// </summary>
    public partial class Stroke : INotifyPropertyChanged
    {
        #region Public APIs


        #region Public Methods
#if false

        /// <summary>
        /// Computes the bounds of the stroke in the default rendering context
        /// </summary>
        /// <returns></returns>
        public virtual Rect GetBounds()
        {
            if (_cachedBounds.IsEmpty)
            {
                StrokeNodeIterator iterator = StrokeNodeIterator.GetIterator(this, this.DrawingAttributes);
                for (int i = 0; i < iterator.Count; i++)
                {
                    StrokeNode strokeNode = iterator[i];
                    _cachedBounds.Union(strokeNode.GetBounds());
                }
            }

            return _cachedBounds;
        }


        /// <summary>
        /// Render the Stroke under the specified DrawingContext. The draw method is a
        /// batch operationg that uses the rendering methods exposed off of DrawingContext
        /// </summary>
        /// <param name="context"></param>
        public void Draw(DrawingContext context)
        {
            if (null == context)
            {
                throw new System.ArgumentNullException("context");
            }

            //our code never calls this public API so we can assume that opacity
            //has not been set up

            //call our public Draw method with the strokes.DA
            this.Draw(context, this.DrawingAttributes);
        }


        /// <summary>
        /// Render the StrokeCollection under the specified DrawingContext. This draw method uses the
        /// passing in drawing attribute to override that on the stroke.
        /// </summary>
        /// <param name="drawingContext"></param>
        /// <param name="drawingAttributes"></param>
        public void Draw(DrawingContext drawingContext, DrawingAttributes drawingAttributes)
        {
            if (null == drawingContext)
            {
                throw new System.ArgumentNullException("context");
            }

            if (null == drawingAttributes)
            {
                throw new System.ArgumentNullException("drawingAttributes");
            }

            //             context.VerifyAccess();

            //our code never calls this public API so we can assume that opacity
            //has not been set up

            if (drawingAttributes.IsHighlighter)
            {
                drawingContext.PushOpacity(StrokeRenderer.HighlighterOpacity);
                try
                {
                    this.DrawInternal(drawingContext, StrokeRenderer.GetHighlighterAttributes(this, this.DrawingAttributes), false);
                }
                finally
                {
                    drawingContext.Pop();
                }
            }
            else
            {
                this.DrawInternal(drawingContext, drawingAttributes, false);
            }
        }

#endif






#endregion

#endregion


        #region Internal APIs




        /// <summary>
        /// Used by Inkcanvas to draw selected stroke as hollow.
        /// </summary>
        [FriendAccessAllowed] // Built into Core, also used by Framework.
        internal bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (value != _isSelected)
                {
                    _isSelected = value;

                    // Raise Invalidated event. This will cause Renderer to repaint and call back DrawCore
                    OnInvalidated(EventArgs.Empty);
                }
            }
        }


        #region Private fields


        private bool                    _isSelected         = false;
        private bool                    _drawAsHollow       = false;
        private bool                    _cloneStylusPoints  = true;
        private bool                    _delayRaiseInvalidated  = false;
        private static readonly double  HollowLineSize      = 1.0f;
        private Rect                    _cachedBounds       = Rect.Empty;

        // The private PropertyChanged event
        private PropertyChangedEventHandler _propertyChanged;

        private const string DrawingAttributesName = "DrawingAttributes";
        private const string StylusPointsName = "StylusPoints";

        #endregion

        internal static readonly double PercentageTolerance = 0.0001d;
        #endregion
    }
}
