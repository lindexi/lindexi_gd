using System;
using System.Collections.Generic;
using System.Globalization;
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

namespace HelrayacalLigemleacaifeece
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Foo.Render += Foo_Render;
        }

        private void Foo_Render(object sender, EventArgs e)
        {
            var dateTime = DateTime.Now;
            Text.Text = $"{dateTime.Hour}:{dateTime.Minute}:{dateTime.Second}";
        }

        private void Button_OnClick(object sender, RoutedEventArgs e)
        {
            Foo.InvalidateVisual();
        }

        private void Show_OnClick(object sender, RoutedEventArgs e)
        {
            Foo.Visibility = Visibility.Visible;
        }

        private void Collapsed_OnClick(object sender, RoutedEventArgs e)
        {
            Foo.Visibility = Visibility.Collapsed;
        }

        private void Hidden_OnClick(object sender, RoutedEventArgs e)
        {
            Foo.Visibility = Visibility.Hidden;
        }
    }

    public class Foo : FrameworkElement
    {
        public event EventHandler Render;

        /// <inheritdoc />
        protected override void OnRender(DrawingContext drawingContext)
        {
            Render?.Invoke(this, null);

            var formattedText = new FormattedText($"lindexi", CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface(new FontFamily("微软雅黑"), new FontStyle(), new FontWeight(), new FontStretch()), 25,
                new SolidColorBrush(Colors.Black), 96);

            drawingContext.DrawText(formattedText, new Point());
            base.OnRender(drawingContext);
        }
    }
}