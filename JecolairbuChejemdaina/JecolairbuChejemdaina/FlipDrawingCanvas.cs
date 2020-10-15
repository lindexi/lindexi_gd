using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace JecolairbuChejemdaina
{
    /// <summary>
    ///     对称图形手绘画板
    /// </summary>
    public class FlipDrawingCanvas : UserControl
    {
        public FlipDrawingCanvas()
        {
            StylusDown += FlipDrawingCanvas_StylusDown;
            StylusMove += FlipDrawingCanvas_StylusMove;
            StylusUp += FlipDrawingCanvas_StylusUp;

            MouseDown += FlipDrawingCanvas_MouseDown;
            MouseMove += FlipDrawingCanvas_MouseMove;
            MouseUp += FlipDrawingCanvas_MouseUp;

            RootGrid = new Grid();

            Content = RootGrid;

            Loaded += FlipDrawingCanvas_Loaded;
        }

        public static readonly DependencyProperty DrawingAttributesProperty = DependencyProperty.Register(
            "DrawingAttributes", typeof(DrawingAttributes), typeof(FlipDrawingCanvas), new PropertyMetadata(
                new DrawingAttributes
                {
                    Color = Colors.Red,
                    FitToCurve = true,
                    Width = 5,
                    Height = 5
                }));

        public static readonly DependencyProperty StrokeVisualListProperty = DependencyProperty.RegisterAttached(
            "StrokeVisualList", typeof(Dictionary<int, StrokeVisual>), typeof(FlipDrawingCanvas),
            new PropertyMetadata(default(Dictionary<int, StrokeVisual>)));

        public DrawingAttributes DrawingAttributes
        {
            get => (DrawingAttributes) GetValue(DrawingAttributesProperty);
            set => SetValue(DrawingAttributesProperty, value);
        }

        public Grid RootGrid { get; }

        public void CleanInk()
        {
            var userControl = (UserControl) RootGrid.Children[0];

            foreach (var panel in ((Panel) userControl.Content).Children.OfType<Panel>())
            {
                panel.Children.Clear();
            }
        }

        private Grid[] GridList { set; get; }
        private bool _isMouseDown;


        private void FlipDrawingCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.StylusDevice != null)
                // 触摸，忽略
                return;

            _isMouseDown = true;

            var position = e.GetPosition(this);
            var id = 0;
            DrawInk(id, new List<Point>
            {
                new Point(position.X, position.Y)
            });
        }

        private void FlipDrawingCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.StylusDevice != null)
                // 触摸，忽略
                return;

            if (_isMouseDown)
            {
                var position = e.GetPosition(this);
                var id = 0;
                DrawInk(id, new List<Point>
                {
                    new Point(position.X, position.Y)
                });
            }
        }

        private void FlipDrawingCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.StylusDevice != null)
                // 触摸，忽略
                return;

            if (_isMouseDown)
            {
                var position = e.GetPosition(this);
                var id = 0;
                DrawInk(id, new List<Point>
                {
                    new Point(position.X, position.Y)
                });
                RemoveCollectionInkId(id);
            }

            _isMouseDown = false;
        }

        private void FlipDrawingCanvas_StylusDown(object sender, StylusDownEventArgs e)
        {
            var position = e.GetPosition(this);
            DrawInk(e.StylusDevice.Id, new List<Point>
            {
                new Point(position.X, position.Y)
            });
        }

        private void FlipDrawingCanvas_StylusMove(object sender, StylusEventArgs e)
        {
            var stylusPointCollection = e.GetStylusPoints(this);
            var id = e.StylusDevice.Id;

            DrawInk(id, stylusPointCollection.Select(temp => new Point(temp.X, temp.Y)).ToList());
        }

        private void FlipDrawingCanvas_StylusUp(object sender, StylusEventArgs e)
        {
            var position = e.GetPosition(this);
            var id = e.StylusDevice.Id;
            DrawInk(id, new List<Point>
            {
                new Point(position.X, position.Y)
            });

            RemoveCollectionInkId(id);
        }

        private void RemoveCollectionInkId(int id)
        {
            foreach (var grid in GridList)
            {
                var strokeVisualList = GetStrokeVisualList(grid);
                if (strokeVisualList == null)
                {
                    strokeVisualList = new Dictionary<int, StrokeVisual>();
                    SetStrokeVisualList(grid, strokeVisualList);
                }

                // 不会移除视觉树
                strokeVisualList.Remove(id);
            }
        }

        private void DrawInk(int id, List<Point> stylusPointCollection)
        {
            foreach (var grid in GridList) DrawInkToGrid(grid, id, stylusPointCollection);
        }

        private void FlipDrawingCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            GridList = BuildFourGrid();
        }

        private Grid[] BuildFourGrid()
        {
            var fourGridFlipDrawingControl = new FourGridFlipDrawingControl();
            RootGrid.Children.Clear();
            RootGrid.Children.Add(fourGridFlipDrawingControl);

            return new[]
            {
                fourGridFlipDrawingControl.Grid1,
                fourGridFlipDrawingControl.Grid2,
                fourGridFlipDrawingControl.Grid3,
                fourGridFlipDrawingControl.Grid4
            };
        }

        private void DrawInkToGrid(Grid grid, int id, List<Point> stylusPointCollection)
        {
            var strokeVisual = GetStrokeVisual(id, grid);

            foreach (var stylusPoint in stylusPointCollection)
                strokeVisual.Add(new StylusPoint(stylusPoint.X, stylusPoint.Y));

            strokeVisual.Redraw();
        }

        private StrokeVisual GetStrokeVisual(int id, Panel grid)
        {
            var strokeVisualList = GetStrokeVisualList(grid);
            if (strokeVisualList == null)
            {
                strokeVisualList = new Dictionary<int, StrokeVisual>();
                SetStrokeVisualList(grid, strokeVisualList);
            }

            if (strokeVisualList.TryGetValue(id, out var visual)) return visual;

            var strokeVisual = new StrokeVisual(DrawingAttributes);
            strokeVisualList[id] = strokeVisual;
            var visualCanvas = new VisualCanvas(strokeVisual);
            grid.Children.Add(visualCanvas);

            return strokeVisual;
        }

        public static void SetStrokeVisualList(DependencyObject element, Dictionary<int, StrokeVisual> value)
        {
            element.SetValue(StrokeVisualListProperty, value);
        }

        public static Dictionary<int, StrokeVisual> GetStrokeVisualList(DependencyObject element)
        {
            return (Dictionary<int, StrokeVisual>) element.GetValue(StrokeVisualListProperty);
        }
    }
}