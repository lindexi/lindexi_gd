using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace CairjawworalhulalGeacharkucoha
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

        private void Grid_OnDrop(object sender, DragEventArgs e)
        {
            var fileList = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (fileList != null)
            {
                if (File.Exists(fileList[0]))
                {
                    var file = fileList[0];
                    if (Path.GetExtension(file).ToLower(CultureInfo.InvariantCulture) == ".gif")
                    {
                        var gifImage = new GifImage();
                        gifImage.GifSource = new Uri(file);

                        Grid.Children.Clear();
                        Grid.Children.Add(gifImage);
                        gifImage.StartAnimation();
                    }
                }
            }
        }
    }
}
