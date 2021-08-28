using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

namespace CalbuhewaNallrolayrani
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.InvokeAsync(() =>
            {
                MyUserControl.IsInVisualBrush();
            });
        }

        private void Button_OnClick(object sender, RoutedEventArgs e)
        {
            Grid.Children.Remove(Border);

            MyUserControl.IsInVisualBrush();
        }
    }

    class MyUserControl : UserControl
    {
        public bool IsInVisualBrush()
        {
            return GetVisualBrushes().Any();
        }

        private List<VisualBrush> GetVisualBrushes()
        {
            var type = typeof(Visual);
            var cyclicBrushToChannelsMapField = type.GetField("CyclicBrushToChannelsMapField", BindingFlags.Static | BindingFlags.NonPublic);
            var cyclicBrushToChannelsMap = cyclicBrushToChannelsMapField.GetValue(null);

            var getValueMethod = cyclicBrushToChannelsMap.GetType().GetMethod("GetValue");
            var cyclicBrushToChannelsMapDictionary = getValueMethod.Invoke(cyclicBrushToChannelsMap, new object[] { this });
            var dictionary = cyclicBrushToChannelsMapDictionary as IDictionary;

            var visualBrushes = dictionary?.Keys.OfType<VisualBrush>().ToList() ?? new List<VisualBrush>(0);
            return visualBrushes;
        }
    }
}
