using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace WaveLineDemo
{
    public class WaveLine : UIElement
    {
        /// <inheritdoc />
        public WaveLine()
        {
            _waveLineList = new VisualCollection(this);
        }

        public double BorderThickness { get; set; } = 1.0;
        public double WaveLength { get; set; } = 4;
        public double WaveHeight { get; set; } = 5;

        public double CurveSquaring { get; set; } = 0.57;

        /// <inheritdoc />
        protected override int VisualChildrenCount => _waveLineList.Count;

        public void DrawWaveLine(Point startPoint, Point endPoint)
        {
            var p1 = startPoint;
            var p2 = endPoint;

            var distance = (p1 - p2).Length;

            var angle = CalculateAngle(p1, p2);
            var waveLength = WaveLength;
            var waveHeight = WaveHeight;
            var howManyWaves = distance / waveLength;
            var waveInterval = distance / howManyWaves;
            var maxBcpLength =
                Math.Sqrt(waveInterval / 4.0 * (waveInterval / 4.0) + waveHeight / 2.0 * (waveHeight / 2.0));

            var curveSquaring = CurveSquaring;
            var bcpLength = maxBcpLength * curveSquaring;
            var bcpInclination = CalculateAngle(new Point(0, 0), new Point(waveInterval / 4.0, waveHeight / 2.0));

            var wigglePoints = new List<(Point bcpOut, Point bcpIn, Point anchor)>();
            var prevFlexPt = p1;
            var polarity = 1;

            for (var waveIndex = 0; waveIndex < howManyWaves * 2; waveIndex++)
            {
                var bcpOutAngle = angle + bcpInclination * polarity;
                var bcpOut = new Point(prevFlexPt.X + Math.Cos(bcpOutAngle) * bcpLength,
                    prevFlexPt.Y + Math.Sin(bcpOutAngle) * bcpLength);
                var flexPt = new Point(prevFlexPt.X + Math.Cos(angle) * waveInterval / 2.0,
                    prevFlexPt.Y + Math.Sin(angle) * waveInterval / 2.0);
                var bcpInAngle = angle + (Math.PI - bcpInclination) * polarity;
                var bcpIn = new Point(flexPt.X + Math.Cos(bcpInAngle) * bcpLength,
                    flexPt.Y + Math.Sin(bcpInAngle) * bcpLength);

                wigglePoints.Add((bcpOut, bcpIn, flexPt));

                polarity *= -1;
                prevFlexPt = flexPt;
            }

            var streamGeometry = new StreamGeometry();
            using (var streamGeometryContext = streamGeometry.Open())
            {
                streamGeometryContext.BeginFigure(wigglePoints[0].anchor, true, false);

                for (var i = 1; i < wigglePoints.Count; i += 1)
                {
                    var (bcpOut, bcpIn, anchor) = wigglePoints[i];

                    streamGeometryContext.BezierTo(bcpOut, bcpIn, anchor, true, false);
                }
            }

            var visual = new DrawingVisual();
            var fillBrush = Brushes.Transparent;
            var lineBrush = Brushes.DarkSeaGreen;
            var borderThickness = BorderThickness;
            var strokePen = new Pen(lineBrush, borderThickness);
            using (var context = visual.RenderOpen())
            {
                context.DrawGeometry(fillBrush, strokePen, streamGeometry);

                //for (var i = 0; i < wigglePoints.Count; i += 1)
                //{
                //    var (bcpOut, bcpIn, anchor) = wigglePoints[i];

                //    context.DrawEllipse(Brushes.Black, null, anchor, 5, 5);
                //}
            }

            _waveLineList.Add(visual);
        }

        private readonly VisualCollection _waveLineList;

        private static double CalculateAngle(Point p1, Point p2)
        {
            return Math.Atan2(p2.Y - p1.Y, p2.X - p1.X);
        }

        /// <inheritdoc />
        protected override Visual GetVisualChild(int index)
        {
            return _waveLineList[index];
        }
    }
}