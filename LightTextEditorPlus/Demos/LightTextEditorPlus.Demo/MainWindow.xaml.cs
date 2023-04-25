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

using Microsoft.Win32;

namespace LightTextEditorPlus.Demo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            TextEditor.TextEditorCore.AppendText("123123123123123123123123123123123123123\r\n1231231231231231231231231231");
        }

        private void InputButton_OnClick(object sender, RoutedEventArgs e)
        {
            TextEditor.TextEditorCore.EditAndReplace(TextBox.Text);
        }

        private async void DebugButton_OnClick(object sender, RoutedEventArgs e)
        {
#pragma warning disable CS0618
            TextEditor.TextEditorCore.Clear();

            // 给调试使用的按钮，可以在这里编写调试代码
            var count = 0;
            while (count >= 0)
            {
                for (int i = 0; i < 100; i++)
                {
                    TextEditor.TextEditorCore.AppendText(((char) Random.Shared.Next('a', 'z')).ToString());
                    await Task.Delay(10);
                }

                TextEditor.TextEditorCore.AppendText("\r\n");
                await Task.Delay(10);

                count++;

                if (count == 10)
                {
                    TextEditor.TextEditorCore.Clear();

                    count = 0;
                }
            }
#pragma warning restore CS0618
        }

        private void BackspaceButton_OnClick(object sender, RoutedEventArgs e)
        {
            TextEditor.TextEditorCore.Backspace();
        }
    }
}
