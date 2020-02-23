using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ICSharpCode.AvalonEdit.CodeCompletion;

namespace NakacehenaHemqawhearlel
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            TextEditor.TextArea.TextEntered += TextAreaOnTextEntered;

            //TextEditor.Document.Lines[1].
            
        }

        private void TextAreaOnTextEntered(object sender, TextCompositionEventArgs e)
        {
            if (e.Text == ".")
            {
                _completionWindow = new CompletionWindow(TextEditor.TextArea);

                var completionData = _completionWindow.CompletionList.CompletionData;
                completionData.Add(new CompletionData("林德熙是逗比"));
                _completionWindow.Show();

                _completionWindow.Closed += (o, args) => _completionWindow = null;
            }
            else if(e.Text == "\n")
            {
            }
        }

        private CompletionWindow _completionWindow;
    }
}