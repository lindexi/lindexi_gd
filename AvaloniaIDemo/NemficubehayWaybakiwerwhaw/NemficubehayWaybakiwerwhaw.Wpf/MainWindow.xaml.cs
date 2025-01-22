using ColorCode;
using ColorCode.Parsing;
using ColorCode.Styling;

using LightTextEditorPlus;

using System.Drawing;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Document;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;

namespace NemficubehayWaybakiwerwhaw.Wpf;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var code = """
                   using System;

                   namespace NemficubehayWaybakiwerwhaw.Desktop;

                   class Program
                   {
                       public static void Main(string[] args)
                       {
                           Console.WriteLine("Hello, World!");
                       }
                   }
                   """;
        TextEditor.AppendText(code);

        ILanguage language = Languages.CSharp;
        var textEditorCodeColorizer = new TextEditorCodeColorizer(TextEditor, null, null);
        textEditorCodeColorizer.FormatInlines(code, language);
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        var documentManager = TextEditor.TextEditorCore.DocumentManager;
        var stringBuilder = new StringBuilder();
        foreach (var charData in documentManager.GetCharDataRange(TextEditor.TextEditorCore.GetAllDocumentSelection()))
        {
            stringBuilder.Append(charData.CharObject.ToText());
        }
        ILanguage language = Languages.CSharp;
        var textEditorCodeColorizer = new TextEditorCodeColorizer(TextEditor, null, null);
        textEditorCodeColorizer.FormatInlines(stringBuilder.ToString(), language);
    }
}

class TextEditorCodeColorizer : CodeColorizerBase
{
    public TextEditorCodeColorizer(TextEditor textEditor, StyleDictionary styles, ILanguageParser languageParser) : base(styles, languageParser)
    {
        _textEditor = textEditor;
    }

    private readonly TextEditor _textEditor;

    public void FormatInlines(string sourceCode, ILanguage language)
    {
        _textEditor.TextEditorCore.Remove(_textEditor.TextEditorCore.GetAllDocumentSelection());

        languageParser.Parse(sourceCode, language, (parsedSourceCode, captures) => Write(parsedSourceCode, captures));
    }

    protected override void Write(string parsedSourceCode, IList<Scope> scopes)
    {
        SolidColorBrush colorBrush = Brushes.Black;

        if (scopes.Count > 0)
        {
            var name = scopes[0].Name;
            if (name == "Keyword")
            {
                colorBrush = Brushes.Blue;
            }
            else if (name == "String")
            {
                colorBrush = BrushCreator.CreateFromARGB(0xFFD69D7F);
            }
            else if (name == "Number")
            {
                colorBrush = BrushCreator.CreateFromARGB(0xFFADCDA8);
            }
            else
            {

            }
        }

        var runProperty = ((RunProperty) _textEditor.CurrentCaretRunProperty) with
        {
            Foreground = new ImmutableBrush(colorBrush)
        };
        _textEditor.AppendRun(new TextRun(parsedSourceCode, runProperty));
    }
}

public static class BrushCreator
{
    public static SolidColorBrush CreateFromARGB(uint argbHex)
    {
        byte a = (byte) ((argbHex & 0xFF000000) >> 24);
        byte r = (byte) ((argbHex & 0x00FF0000) >> 16);
        byte g = (byte) ((argbHex & 0x0000FF00) >> 8);
        byte b = (byte) (argbHex & 0x000000FF);
        var brush = new SolidColorBrush(Color.FromArgb(a, r, g, b));
        return brush;
    }

}