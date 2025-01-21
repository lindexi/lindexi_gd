using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Interactivity;
using Avalonia.Media;

using ColorCode;
using ColorCode.Parsing;
using ColorCode.Styling;

using LightTextEditorPlus;
using LightTextEditorPlus.Core;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Document;
using SkiaSharp;

namespace NemficubehayWaybakiwerwhaw.Views;

public partial class MainView : UserControl
{
    public MainView()
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
        var documentManager = TextEditor.SkiaTextEditor.TextEditorCore.DocumentManager;
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
        SKColor color = SKColors.Black;

        if (scopes.Count > 0)
        {
            var name = scopes[0].Name;
            if (name == "Keyword")
            {
                color = SKColors.Blue;
            }
            else if(name == "String")
            {
                color = SKColor.Parse("D69D7F");
            }
            else if(name == "Number")
            {
                color = SKColor.Parse("ADCDA8");
            }
            else
            {
                
            }
        }

        _textEditor.AppendRun(new SkiaTextRun(parsedSourceCode, _textEditor.CurrentCaretRunProperty with
        {
            Foreground = color
        }));
    }
}
