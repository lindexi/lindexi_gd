
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using MS.Internal;
using MS.Internal.Ink;

namespace
#if WpfInk
    WpfInk
#else
    WpfInkOld
#endif
{
    public class Test
    {
        public static void CalcGeometryAndBoundsWithTransform()
        {
            var drawingAttribute = new DrawingAttributes();
            var strokeNodeIterator = StrokeNodeIterator.GetIterator(new StylusPointCollection(new Point[]
            {
                new Point(10, 10),
                new Point(11,10), 
                new Point(12,10),
                new Point(13,10),
                new Point(14,10),
                new Point(15,10),
                new Point(15,11),
                new Point(15,12),
                new Point(15,13),
                new Point(15,14),
                new Point(15,15),
                new Point(25,35),
                new Point(35,15),
                new Point(55,25),

            }), drawingAttribute);

            StrokeRenderer.CalcGeometryAndBoundsWithTransform(strokeNodeIterator, drawingAttribute,MatrixTypes.TRANSFORM_IS_IDENTITY,true,out var geometry,out var bounds);
        }
    }
}