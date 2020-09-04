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
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Platform.WPF;

namespace BugReport.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : FormsApplicationPage
    {
        public MainWindow()
        {
            InitializeComponent();

            var font = ToFontFamily("pack://application:,,,/Fonts/#Font Awesome 5 Free Solid");

            var window = new Window()
            {
                Content = new TextBlock()
                {
                    Text = "\uf007;",
                    FontFamily = font,
                    Margin = new System.Windows. Thickness(10,10,10,10)
                }
            };

            window.Show();

            FontRegistrar.Register(new ExportFontAttribute("FontAwesome5_Solid.otf"), typeof(MainWindow).Assembly);

            Forms.Init();
            LoadApplication(new BugReport.App());
        }

        public static FontFamily ToFontFamily(string fontFamily, string defaultFontResource = "FontFamilySemiBold")
        {
            const string packUri = "pack://application:,,,/";
            if (fontFamily.StartsWith(packUri))
            {
                var fontName = fontFamily.Remove(0, packUri.Length);
                return new FontFamily(new Uri(packUri), fontName);
            }

            return null;
        }
    }
}
