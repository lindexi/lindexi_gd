using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using MS.Internal;
using MS.Internal.Ink;

[assembly: InternalsVisibleTo("Benchmark")]

namespace
#if WpfInk
    WpfInk
#elif WpfInkErasingStroke
    WpfInkErasingStroke
#else
    WpfInkOld
#endif
{
    public struct Context
    {
        internal StrokeNodeIterator StrokeNodeIterator { get; set; }
        internal DrawingAttributes DrawingAttributes { get; set; }
    }

    public class ErasingStrokeTest
    {
        public TextContext GetErasingStroke(Point[] pointList)
        {
            var erasingStroke = new ErasingStroke(new RectangleStylusShape(10, 10));
            erasingStroke.MoveTo(pointList);

            var strokeNodeIterator = StrokeNodeIterator.GetIterator(new StylusPointCollection(pointList.Select(temp => new Point(temp.X, temp.Y))),
                new DrawingAttributes()
                {
                    Width = 5,
                    Height = 5
                });

            return new TextContext(strokeNodeIterator, erasingStroke, new List<StrokeIntersection>(4096), this);
        }

        public bool EraseTest(TextContext textContext)
        {
            var erasingStroke = textContext.ErasingStroke;
            return erasingStroke.EraseTest(textContext.Iterator, textContext.IntersectionList);
        }

        public class TextContext
        {
            internal TextContext(StrokeNodeIterator iterator, ErasingStroke erasingStroke,
                List<StrokeIntersection> intersectionList, ErasingStrokeTest erasingStrokeTest)
            {
                Iterator = iterator;
                ErasingStroke = erasingStroke;
                IntersectionList = intersectionList;
                ErasingStrokeTest = erasingStrokeTest;
            }

            public bool EraseTest()
            {
                IntersectionList.Clear();
                return ErasingStrokeTest.EraseTest(this);
            }

            private ErasingStrokeTest ErasingStrokeTest { get; }

            internal StrokeNodeIterator Iterator { get; }

            internal ErasingStroke ErasingStroke { get; }

            internal List<StrokeIntersection> IntersectionList { get; }
        }
    }

    public class Test
    {
        internal static void CalcGeometryAndBoundsWithTransform(Context context)
        {
            StrokeRenderer.CalcGeometryAndBoundsWithTransform(context.StrokeNodeIterator, context.DrawingAttributes,
                MatrixTypes.TRANSFORM_IS_IDENTITY, true, out var geometry, out var bounds);
        }

        internal static Context GetContext(Point[] pointList)
        {
            var drawingAttribute = new DrawingAttributes();
            var strokeNodeIterator =
                StrokeNodeIterator.GetIterator(new StylusPointCollection(pointList), drawingAttribute);

            return new Context()
            {
                StrokeNodeIterator = strokeNodeIterator,
                DrawingAttributes = drawingAttribute
            };
        }

        public static void CalcGeometryAndBoundsWithTransform()
        {
            var drawingAttribute = new DrawingAttributes();
            var strokeNodeIterator = StrokeNodeIterator.GetIterator(new StylusPointCollection(new Point[]
            {
                new Point(10, 10),
                new Point(11, 10),
                new Point(12, 10),
                new Point(13, 10),
                new Point(14, 10),
                new Point(15, 10),
                new Point(15, 11),
                new Point(15, 12),
                new Point(15, 13),
                new Point(15, 14),
                new Point(15, 15),
                new Point(25, 35),
                new Point(35, 15),
                new Point(55, 25),
            }), drawingAttribute);

            StrokeRenderer.CalcGeometryAndBoundsWithTransform(strokeNodeIterator, drawingAttribute,
                MatrixTypes.TRANSFORM_IS_IDENTITY, true, out var geometry, out var bounds);
        }
    }
}