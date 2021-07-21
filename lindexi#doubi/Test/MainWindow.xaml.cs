using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace Test
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var uri = new Uri("Image.png", UriKind.Relative);
            try
            {
                var uriAbsolutePath = uri.AbsolutePath;
            }
            catch (InvalidOperationException e)
            {
                Debug.Assert(e.Message == "This operation is not supported for a relative URI.");
            }

            var streamResourceInfo = Application.GetContentStream(uri);
        }
    }
}
