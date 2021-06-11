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

namespace JebegeldalfearqairwelLerjerehacawhem
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
            var type = typeof(Application).Assembly.GetType("MS.Internal.AppModel.MimeObjectFactory")!;
            var field = type!.GetField("_objectConverters", BindingFlags.NonPublic | BindingFlags.Static);
            var objectConverters = field!.GetValue(null);
            var dictionary = (IDictionary) objectConverters;
            var count = dictionary!.Count;
        }
    }
}