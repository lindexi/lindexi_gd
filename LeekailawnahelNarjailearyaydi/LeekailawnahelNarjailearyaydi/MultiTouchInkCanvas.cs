using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace LeekailawnahelNarjailearyaydi
{
    public class MultiTouchInkCanvas : Grid
    {
        public MultiTouchInkCanvas()
        {
            Background = Brushes.Transparent;

            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;

            StylusMove += MultiTouchInkCanvas_StylusMove;
            StylusUp += MultiTouchInkCanvas_StylusUp;
        }

        private void MultiTouchInkCanvas_StylusUp(object sender, StylusEventArgs e)
        {
            StrokeVisualList.Remove(e.StylusDevice.Id);
        }

        private void MultiTouchInkCanvas_StylusMove(object sender, StylusEventArgs e)
        {
            var strokeVisual = GetStrokeVisual(e.StylusDevice.Id);
            var stylusPointCollection = e.GetStylusPoints(this);
            foreach (var stylusPoint in stylusPointCollection)
            {
                strokeVisual.Add(new StylusPoint(stylusPoint.X, stylusPoint.Y));
            }

            strokeVisual.Redraw();
        }

        public Grid Grid => this;

        private Dictionary<int, StrokeVisual> StrokeVisualList { get; } = new Dictionary<int, StrokeVisual>();

        private StrokeVisual GetStrokeVisual(int id)
        {
            if (StrokeVisualList.TryGetValue(id, out var visual))
            {
                return visual;
            }

            var strokeVisual = new StrokeVisual();
            StrokeVisualList[id] = strokeVisual;
            var visualCanvas = new VisualCanvas(strokeVisual);
            Grid.Children.Add(visualCanvas);

            return strokeVisual;
        }
    }


    /// <summary>
    ///     用于显示笔迹的类
    /// </summary>
    public class StrokeVisual : DrawingVisual
    {
        /// <summary>
        ///     创建显示笔迹的类
        /// </summary>
        public StrokeVisual() : this(new DrawingAttributes()
        {
            Color = Colors.Red,
            FitToCurve = true,
            Width = 5
        })
        {
        }

        /// <summary>
        ///     创建显示笔迹的类
        /// </summary>
        /// <param name="drawingAttributes"></param>
        public StrokeVisual(DrawingAttributes drawingAttributes)
        {
            _drawingAttributes = drawingAttributes;
        }

        private readonly DrawingAttributes _drawingAttributes;

        /// <summary>
        ///     设置或获取显示的笔迹
        /// </summary>
        public Stroke Stroke { set; get; }

        /// <summary>
        ///     在笔迹中添加点
        /// </summary>
        /// <param name="point"></param>
        public void Add(StylusPoint point)
        {
            if (Stroke == null)
            {
                var collection = new StylusPointCollection {point};
                Stroke = new Stroke(collection) {DrawingAttributes = _drawingAttributes};
            }
            else
            {
                Stroke.StylusPoints.Add(point);
            }
        }

        /// <summary>
        ///     重新画出笔迹
        /// </summary>
        public void Redraw()
        {
            using var dc = RenderOpen();
            Stroke.Draw(dc);
        }
    }

    public class VisualCanvas : FrameworkElement
    {
        protected override Visual GetVisualChild(int index)
        {
            return Visual;
        }

        protected override int VisualChildrenCount => 1;

        public VisualCanvas(DrawingVisual visual)
        {
            Visual = visual;
            AddVisualChild(visual);
        }

        public DrawingVisual Visual { get; }
    }
}