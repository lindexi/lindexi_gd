using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LeajemhurhoCaiwhemqurhahawwhaw
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Board_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            var position = e.GetPosition(this);

            DrawEllipse(position);

            PointList.Add(position);

            var polygon = new Polygon()
            {
                Points = new PointCollection(PointList),
                Stroke = Brushes.Red,
            };

            AddElement(nameof(polygon), polygon);

            if (e.RightButton == MouseButtonState.Pressed)
            {
                CleanBoard();

                PointList.Clear();
            }
        }

        private List<Point> PointList { get; } = new List<Point>();

        private T GetElement<T>(string id) where T : UIElement
        {
            var element = Board.Children.OfType<UIElement>().FirstOrDefault(temp => id.Equals(GetId(temp)));
            return (T) element;
        }

        private void AddElement(string id, UIElement element)
        {
            RemoveElementById(id);
            Board.Children.Add(element);
            SetId(element, id);
        }

        private void RemoveElementById(string id)
        {
            var element = Board.Children.OfType<UIElement>().FirstOrDefault(temp => id.Equals(GetId(temp)));
            Board.Children.Remove(element);
        }

        private void CleanBoard()
        {
            Board.Children.Clear();
        }

        private void DrawEllipse(Point position)
        {
            var ellipse = new Ellipse()
            {
                Width = 5,
                Height = 5,
                Stroke = Brushes.Red,
                RenderTransform = new TranslateTransform(position.X, position.Y)
            };

            Board.Children.Add(ellipse);
        }

        public static readonly DependencyProperty IdProperty = DependencyProperty.RegisterAttached(
            "Id", typeof(string), typeof(MainWindow), new PropertyMetadata(default(string)));

        public static void SetId(DependencyObject element, string value)
        {
            element.SetValue(IdProperty, value);
        }

        public static string GetId(DependencyObject element)
        {
            return (string) element.GetValue(IdProperty);
        }
    }

    static class DoHelper
    {
        public static T Do<T>(this T t, Action<T> action)
        {
            action(t);
            return t;
        }
    }
}