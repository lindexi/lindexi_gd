using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace NiwejabainelFehargaye
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

        private void TextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            TextEditor.Text = ((TextBox)sender).Text;
            TextEditor.InvalidateVisual();
        }
    }
}
