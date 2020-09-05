using System;
using System.Collections.Generic;
using System.IO;
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
using DocumentFormat.OpenXml.Packaging;

namespace JallweawhairnoFairwherenajajal
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

        private void UIElement_OnDragEnter(object sender, DragEventArgs e)
        {
            e.Handled = true;
            var fileList = (string[]) e.Data.GetData("FileDrop");

            using (FileStream fs = new FileStream(fileList[0], FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var doc = WordprocessingDocument.Open(fs, false))
                {
                    var mainDocumentPart = doc.MainDocumentPart;
                    var body = mainDocumentPart.Document.Body;
                    Console.WriteLine(body.InnerText);
                }
            }
        }
    }
}