using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace KelerbelaChukoqayhi
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            StylusDown += MainWindow_StylusDown;
            StylusMove += MainWindow_StylusMove;
            StylusUp += MainWindow_StylusUp;
        }

        private void MainWindow_StylusDown(object sender, StylusDownEventArgs e)
        {
            InkRecordUserControl.InkDataModelCollection.Clear();
            _lastTime = DateTime.Now;
        }

        private DateTime _lastTime;

        private void MainWindow_StylusMove(object sender, StylusEventArgs e)
        {
            var time = DateTime.Now - _lastTime;

            var stylusPointCollection = e.GetStylusPoints(this);
            foreach (var stylusPoint in stylusPointCollection)
            {
                InkRecordUserControl.InkDataModelCollection.Add(new InkDataModel(stylusPoint, time));
            }

            var strokeVisual = GetStrokeVisual(e.StylusDevice.Id);
            foreach (var stylusPoint in stylusPointCollection)
            {
                strokeVisual.Add(new StylusPoint(stylusPoint.X, stylusPoint.Y));
            }

            strokeVisual.Redraw();
        }

        private void MainWindow_StylusUp(object sender, StylusEventArgs e)
        {
            StrokeVisualList.Remove(e.StylusDevice.Id);
            StrokeDataModelList.Add(new StrokeDataModel(InkRecordUserControl.InkDataModelCollection));

            StrokeCount = StrokeDataModelList.Count;
        }

        private StrokeDataModelList StrokeDataModelList { get; } = new StrokeDataModelList();

        private void SaveInkButton_OnClick(object sender, RoutedEventArgs e)
        {
            var text = StrokeDataModelList.Serialize();

            var saveFileDialog = new SaveFileDialog();
            saveFileDialog.FileName = "Ink.txt";
            saveFileDialog.ShowDialog(this);

            if (string.IsNullOrEmpty(saveFileDialog.FileName))
            {
                return;
            }

            var file = System.IO.Path.GetFullPath(saveFileDialog.FileName);
            var directoryName = System.IO.Path.GetDirectoryName(file);
            System.IO.Directory.CreateDirectory(directoryName);

            System.IO.File.WriteAllText(file, text);

            var strokeDataModelList = StrokeDataModelList.Deserialize(text);
        }

        public static readonly DependencyProperty StrokeCountProperty = DependencyProperty.Register(
            "StrokeCount", typeof(int), typeof(MainWindow), new PropertyMetadata(default(int)));

        public int StrokeCount
        {
            get { return (int) GetValue(StrokeCountProperty); }
            set { SetValue(StrokeCountProperty, value); }
        }

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

        private Dictionary<int, StrokeVisual> StrokeVisualList { get; } = new Dictionary<int, StrokeVisual>();

       
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
                var collection = new StylusPointCollection { point };
                Stroke = new Stroke(collection) { DrawingAttributes = _drawingAttributes };
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

        private readonly DrawingAttributes _drawingAttributes;
    }
}
