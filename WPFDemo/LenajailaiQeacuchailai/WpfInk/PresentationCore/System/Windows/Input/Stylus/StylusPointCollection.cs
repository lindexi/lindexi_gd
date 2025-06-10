// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;

namespace WpfInk.PresentationCore.System.Windows.Input.Stylus
{
    /// <summary>
    /// StylusPointCollection
    /// </summary>
    internal class StylusPointCollection : Collection<StylusPoint>
    {
        /// <summary>
        /// Changed event, anytime the data in this collection changes, this event is raised
        /// </summary>
        public event EventHandler? Changed;

        /// <summary>
        /// Internal only changed event used by Stroke to prevent zero count strokes
        /// </summary>
        internal event CancelEventHandler? CountGoingToZero;

        /// <summary>
        /// StylusPointCollection
        /// </summary>
        public StylusPointCollection()
        {
        }

        /// <summary>
        /// StylusPointCollection
        /// </summary>
        /// <param name="initialCapacity">initialCapacity</param>
        public StylusPointCollection(int initialCapacity)
            : this()
        {
            if (initialCapacity < 0)
            {
                throw new ArgumentException(SR.InvalidStylusPointConstructionZeroLengthCollection, nameof(initialCapacity));
            }
            ((List<StylusPoint>) this.Items).Capacity = initialCapacity;
        }


        /// <summary>
        /// StylusPointCollection
        /// </summary>
        /// <param name="stylusPointDescription">stylusPointDescription</param>
        /// <param name="initialCapacity">initialCapacity</param>
        public StylusPointCollection(StylusPointDescription stylusPointDescription, int initialCapacity)
            : this()
        {
            if (initialCapacity < 0)
            {
                throw new ArgumentException(SR.InvalidStylusPointConstructionZeroLengthCollection, nameof(initialCapacity));
            }
            ((List<StylusPoint>) this.Items).Capacity = initialCapacity;
        }


        /// <summary>
        /// StylusPointCollection
        /// </summary>
        /// <param name="points">points</param>
        public StylusPointCollection(IEnumerable<Point> points)
            : this()
        {
            ArgumentNullException.ThrowIfNull(points);

            List<StylusPoint> stylusPoints = new List<StylusPoint>();
            foreach (Point point in points)
            {
                //this can throw (since point.X or Y can be beyond our range)
                //don't add to our internal collection until after we instance
                //all of the styluspoints and we know the ranges are valid
                stylusPoints.Add(new StylusPoint(point.X, point.Y));
            }

            if (stylusPoints.Count == 0)
            {
                throw new ArgumentException(SR.InvalidStylusPointConstructionZeroLengthCollection, nameof(points));
            }

            ((List<StylusPoint>) this.Items).Capacity = stylusPoints.Count;
            ((List<StylusPoint>) this.Items).AddRange(stylusPoints);
        }

#if false
        
        /// <summary>
        /// Internal ctor called by input with a raw int[]
        /// </summary>
        /// <param name="stylusPointDescription">stylusPointDescription</param>
        /// <param name="rawPacketData">rawPacketData</param>
        /// <param name="tabletToView">tabletToView</param>
        /// <param name="tabletToViewMatrix">tabletToView</param>
        internal StylusPointCollection(StylusPointDescription stylusPointDescription, int[] rawPacketData, GeneralTransform tabletToView, Matrix tabletToViewMatrix)
        {
            ArgumentNullException.ThrowIfNull(stylusPointDescription);
            _stylusPointDescription = stylusPointDescription;

            int lengthPerPoint = stylusPointDescription.GetInputArrayLengthPerPoint();
            int logicalPointCount = rawPacketData.Length / lengthPerPoint;
            Debug.Assert(0 == rawPacketData.Length % lengthPerPoint, "Invalid assumption about packet length, there shouldn't be any remainder");

            //
            // set our capacity and validate
            //
            ((List<StylusPoint>) this.Items).Capacity = logicalPointCount;
            for (int count = 0, i = 0; count < logicalPointCount; count++, i += lengthPerPoint)
            {
                //first, determine the x, y values by xf-ing them
                Point p = new Point(rawPacketData[i], rawPacketData[i + 1]);
                if (tabletToView != null)
                {
                    tabletToView.TryTransform(p, out p);
                }
                else
                {
                    p = tabletToViewMatrix.Transform(p);
                }

                int startIndex = 2;
                bool containsTruePressure = stylusPointDescription.ContainsTruePressure;
                if (containsTruePressure)
                {
                    //don't copy pressure in the int[] for extra data
                    startIndex++;
                }

                int[] data = null;
                int dataLength = lengthPerPoint - startIndex;
                if (dataLength > 0)
                {
                    //copy the rest of the data
                    var rawArrayStartIndex = i + startIndex;
                    data = rawPacketData.AsSpan(rawArrayStartIndex, dataLength).ToArray();
                }

                StylusPoint newPoint = new StylusPoint(p.X, p.Y, StylusPoint.DefaultPressure, _stylusPointDescription, data, false, false);
                if (containsTruePressure)
                {
                    //use the algorithm to set pressure in StylusPoint
                    int pressure = rawPacketData[i + 2];
                    newPoint.SetPropertyValue(StylusPointProperties.NormalPressure, pressure);
                }

                //this does not go through our protected virtuals
                ((List<StylusPoint>) this.Items).Add(newPoint);
            }
        }
#endif

        /// <summary>
        /// Adds the StylusPoints in the StylusPointCollection to this StylusPointCollection
        /// </summary>
        /// <param name="stylusPoints">stylusPoints</param>
        public void Add(StylusPointCollection stylusPoints)
        {
            //note that we don't raise an exception if stylusPoints.Count == 0
            ArgumentNullException.ThrowIfNull(stylusPoints);
         

            // cache count outside of the loop, so if this SPC is ever passed
            // we don't loop forever
            int count = stylusPoints.Count;
            for (int x = 0; x < count; x++)
            {
                StylusPoint stylusPoint = stylusPoints[x];
                //this does not go through our protected virtuals
                ((List<StylusPoint>) this.Items).Add(stylusPoint);
            }

            if (stylusPoints.Count > 0)
            {
                OnChanged(EventArgs.Empty);
            }
        }

        /// <summary>
        /// called by base class Collection&lt;T&gt; when the list is being cleared;
        /// raises a CollectionChanged event to any listeners
        /// </summary>
        protected sealed override void ClearItems()
        {
            if (CanGoToZero())
            {
                base.ClearItems();
                OnChanged(EventArgs.Empty);
            }
            else
            {
                throw new InvalidOperationException(SR.InvalidStylusPointCollectionZeroCount);
            }
        }

        /// <summary>
        /// called by base class Collection&lt;T&gt; when an item is removed from list;
        /// raises a CollectionChanged event to any listeners
        /// </summary>
        protected sealed override void RemoveItem(int index)
        {
            if (this.Count > 1 || CanGoToZero())
            {
                base.RemoveItem(index);
                OnChanged(EventArgs.Empty);
            }
            else
            {
                throw new InvalidOperationException(SR.InvalidStylusPointCollectionZeroCount);
            }
        }

        /// <summary>
        /// called by base class Collection&lt;T&gt; when an item is added to list;
        /// raises a CollectionChanged event to any listeners
        /// </summary>
        protected sealed override void InsertItem(int index, StylusPoint stylusPoint)
        {
            base.InsertItem(index, stylusPoint);

            OnChanged(EventArgs.Empty);
        }

        /// <summary>
        /// called by base class Collection&lt;T&gt; when an item is set in list;
        /// raises a CollectionChanged event to any listeners
        /// </summary>
        protected sealed override void SetItem(int index, StylusPoint stylusPoint)
        {
            base.SetItem(index, stylusPoint);

            OnChanged(EventArgs.Empty);
        }

        /// <summary>
        /// Clone
        /// </summary>
        public StylusPointCollection Clone()
        {
            return this.Clone(/*System.Windows.Media.Transform.Identity,*/ /*this.Description,*/ this.Count);
        }

        /// <summary>
        /// Explicit cast converter between StylusPointCollection and Point[]
        /// </summary>
        /// <param name="stylusPoints">stylusPoints</param>
        public static explicit operator Point[](StylusPointCollection stylusPoints)
        {
            if (stylusPoints == null)
            {
                return null;
            }

            Point[] points = new Point[stylusPoints.Count];
            for (int i = 0; i < stylusPoints.Count; i++)
            {
                points[i] = new Point(stylusPoints[i].X, stylusPoints[i].Y);
            }
            return points;
        }

#if false
        /// <summary>
        /// Clone and truncate
        /// </summary>
        /// <param name="count">The maximum count of points to clone (used by GestureRecognizer)</param>
        /// <returns></returns>
        internal StylusPointCollection Clone(int count)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(count, this.Count);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

            return this.Clone(System.Windows.Media.Transform.Identity, this.Description, count);
        }

        /// <summary>
        /// Clone with a transform, used by input
        /// </summary>
        internal StylusPointCollection Clone(GeneralTransform transform, StylusPointDescription descriptionToUse)
        {
            return this.Clone(transform, descriptionToUse, this.Count);
        }
#endif


        /// <summary>
        /// Private clone implementation
        /// </summary>
        private StylusPointCollection Clone(/*GeneralTransform transform,*/ /*StylusPointDescription descriptionToUse,*/ int count)
        {
            Debug.Assert(count <= this.Count);
            //
            // We don't need to copy our _stylusPointDescription because it is immutable
            // and we don't need to copy our StylusPoints, because they are structs.
            //
            StylusPointCollection newCollection =
                new StylusPointCollection(count);

            bool isIdentity = //(transform is Transform) ? ((Transform) transform).IsIdentity : false;
                true;
            for (int x = 0; x < count; x++)
            {
                if (isIdentity)
                {
                    ((List<StylusPoint>) newCollection.Items).Add(this[x]);
                }
                else
                {
#if false
                    Point point = new Point();
                    StylusPoint stylusPoint = this[x];
                    point.X = stylusPoint.X;
                    point.Y = stylusPoint.Y;
                    transform.TryTransform(point, out point);
                    stylusPoint.X = point.X;
                    stylusPoint.Y = point.Y;
                    ((List<StylusPoint>) newCollection.Items).Add(stylusPoint);
#endif
                    throw new NotImplementedException();
                }
            }
            return newCollection;
        }

        /// <summary>
        /// Protected virtual for raising changed notification
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnChanged(EventArgs e)
        {
            ArgumentNullException.ThrowIfNull(e);
            if (this.Changed != null)
            {
                this.Changed(this, e);
            }
        }

#if false
        /// <summary>
        /// Transform the StylusPoints in this collection by the specified transform
        /// </summary>
        /// <param name="transform">transform</param>
        internal void Transform(GeneralTransform transform)
        {
            Point point = new Point();
            for (int i = 0; i < this.Count; i++)
            {
                StylusPoint stylusPoint = this[i];
                point.X = stylusPoint.X;
                point.Y = stylusPoint.Y;
                transform.TryTransform(point, out point);
                stylusPoint.X = point.X;
                stylusPoint.Y = point.Y;

                //this does not go through our protected virtuals
                ((List<StylusPoint>) this.Items)[i] = stylusPoint;
            }

            if (this.Count > 0)
            {
                this.OnChanged(EventArgs.Empty);
            }
        }
        /// <summary>
        /// Reformat
        /// </summary>
        /// <param name="subsetToReformatTo">subsetToReformatTo</param>
        public StylusPointCollection Reformat(StylusPointDescription subsetToReformatTo)
        {
            return Reformat(subsetToReformatTo, System.Windows.Media.Transform.Identity);
        }

        /// <summary>
        /// Helper that transforms and scales in one go
        /// </summary>
        internal StylusPointCollection Reformat(StylusPointDescription subsetToReformatTo, GeneralTransform transform)
        {
            if (!subsetToReformatTo.IsSubsetOf(this.Description))
            {
                throw new ArgumentException(SR.InvalidStylusPointDescriptionSubset, nameof(subsetToReformatTo));
            }

            StylusPointDescription subsetToReformatToWithCurrentMetrics =
                StylusPointDescription.GetCommonDescription(subsetToReformatTo,
                                                            this.Description); //preserve metrics from this spd

            if (StylusPointDescription.AreCompatible(this.Description, subsetToReformatToWithCurrentMetrics) &&
                (transform is Transform) && ((Transform) transform).IsIdentity)
            {
                //subsetToReformatTo might have different x, y, p metrics
                return this.Clone(transform, subsetToReformatToWithCurrentMetrics);
            }

            //
            // we really need to reformat this...
            //
            StylusPointCollection newCollection = new StylusPointCollection(subsetToReformatToWithCurrentMetrics, this.Count);
            int additionalDataCount = subsetToReformatToWithCurrentMetrics.GetExpectedAdditionalDataCount();

            ReadOnlyCollection<StylusPointPropertyInfo> properties
                    = subsetToReformatToWithCurrentMetrics.GetStylusPointProperties();
            bool isIdentity = (transform is Transform) ? ((Transform) transform).IsIdentity : false;

            for (int i = 0; i < this.Count; i++)
            {
                StylusPoint stylusPoint = this[i];

                double xCoord = stylusPoint.X;
                double yCoord = stylusPoint.Y;
                float pressure = stylusPoint.GetUntruncatedPressureFactor();

                if (!isIdentity)
                {
                    Point p = new Point(xCoord, yCoord);
                    transform.TryTransform(p, out p);
                    xCoord = p.X;
                    yCoord = p.Y;
                }

                int[] newData = null;
                if (additionalDataCount > 0)
                {
                    //don't init, we'll do that below
                    newData = new int[additionalDataCount];
                }

                StylusPoint newStylusPoint =
                    new StylusPoint(xCoord, yCoord, pressure, subsetToReformatToWithCurrentMetrics, newData, false, false);

                //start at 3, skipping x, y, pressure
                for (int x = StylusPointDescription.RequiredCountOfProperties/*3*/; x < properties.Count; x++)
                {
                    int value = stylusPoint.GetPropertyValue(properties[x]);
                    newStylusPoint.SetPropertyValue(properties[x], value, copyBeforeWrite: false);
                }
                //bypass validation
                ((List<StylusPoint>) newCollection.Items).Add(newStylusPoint);
            }
            return newCollection;
        }
#endif


        /// <summary>
        /// Private helper use to consult with any listening strokes if it is safe to go to zero count
        /// </summary>
        /// <returns></returns>
        private bool CanGoToZero()
        {
            if (null == this.CountGoingToZero)
            {
                //
                // no one is listening
                //
                return true;
            }

            CancelEventArgs e = new CancelEventArgs
            {
                Cancel = false
            };

            //
            // call the listeners
            //
            this.CountGoingToZero(this, e);
            Debug.Assert(e.Cancel, "This event should always be cancelled");

            return !e.Cancel;
        }
    }
}
