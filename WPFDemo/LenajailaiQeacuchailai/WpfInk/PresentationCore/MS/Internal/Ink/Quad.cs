// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections.Generic;
using WpfInk;
using WpfInk.PresentationCore.System.Windows;

namespace MS.Internal.Ink
{
    /// <summary>
    /// A helper structure used in StrokeNode and StrokeNodeOperation implementations
    /// to store endpoints of the quad connecting two nodes of a stroke. 
    /// The vertices of a quad are supposed to be clockwise with points A and D located
    /// on the begin node and B and C on the end one.
    /// </summary>
    internal struct Quad
    {
        #region Statics

        private static readonly Quad s_empty = new Quad(new InkPoint2D(0, 0), new InkPoint2D(0, 0), new InkPoint2D(0, 0), new InkPoint2D(0, 0));

        #endregion 
        
        #region API

        /// <summary> Returns the static object representing an empty (unitialized) quad </summary>
        internal static Quad Empty { get { return s_empty; } }

        /// <summary> Constructor </summary>
        internal Quad(InkPoint2D a, InkPoint2D b, InkPoint2D c, InkPoint2D d)
        {
            _A = a; _B = b; _C = c; _D = d;
        }

        /// <summary> The A vertex of the quad </summary>
        internal InkPoint2D A { get { return _A; } set { _A = value; } }
        
        /// <summary> The B vertex of the quad </summary>
        internal InkPoint2D B { get { return _B; } set { _B = value; } }
        
        /// <summary> The C vertex of the quad </summary>
        internal InkPoint2D C { get { return _C; } set { _C = value; } }

        /// <summary> The D vertex of the quad </summary>
        internal InkPoint2D D { get { return _D; } set { _D = value; } }

        // Returns quad's vertex by index where A is of the index 0, B - is 1, etc
        internal InkPoint2D this[int index]
        {
            get 
            {
                switch (index)
                {
                    case 0: return _A;
                    case 1: return _B;
                    case 2: return _C;
                    case 3: return _D;
                    default:
                        throw new IndexOutOfRangeException("index");
                }
            }
        }

        /// <summary> Tells whether the quad is invalid (empty) </summary>
        internal bool IsEmpty 
        { 
            get { return (_A == _B) && (_C == _D); } 
        }

        internal void GetPoints(List<InkPoint2D> pointBuffer)
        {
            pointBuffer.Add(_A);
            pointBuffer.Add(_B);
            pointBuffer.Add(_C);
            pointBuffer.Add(_D);
        }
        
        /// <summary> Returns the bounds of the quad </summary>
        internal Rect Bounds 
        {
            get { return IsEmpty ? Rect.Empty : Rect.Union(new Rect(_A, _B), new Rect(_C, _D)); }
        }
        
        #endregion

        #region Fields

        private InkPoint2D _A;
        private InkPoint2D _B;
        private InkPoint2D _C;
        private InkPoint2D _D;

        #endregion
    }
}
