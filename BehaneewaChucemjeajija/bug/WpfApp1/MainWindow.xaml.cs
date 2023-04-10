
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Baml2006;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Xaml;
using Path = System.Windows.Shapes.Path;
using XamlReader = System.Windows.Markup.XamlReader;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            KeyWordTag ta = new KeyWordTag();
            var contet = File.ReadAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "1.txt"));
            var res = XamlReader.Parse(contet) as FlowDocument;
           RichTextBox.Document = res;

           foreach (var documentBlock in RichTextBox.Document.Blocks)
           {
               TextRange textRange = new TextRange(documentBlock.ContentStart, documentBlock.ContentEnd);
               string paragraphText = textRange.Text;
            }
        }
    }
}
