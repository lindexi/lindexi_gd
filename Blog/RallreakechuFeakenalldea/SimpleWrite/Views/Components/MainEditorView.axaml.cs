using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

using LightTextEditorPlus;
using LightTextEditorPlus.Primitive;

using SimpleWrite.ViewModels;

using SkiaSharp;

using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Input;
using LightTextEditorPlus.Editing;
using SimpleWrite.Models;

namespace SimpleWrite.Views.Components;

public partial class MainEditorView : UserControl
{
    public MainEditorView()
    {
        InitializeComponent();
    }

    public EditorViewModel ViewModel => DataContext as EditorViewModel
        ?? throw new InvalidOperationException("DataContext must be of type EditorViewModel");

    protected override void OnDataContextChanged(EventArgs e)
    {
        if (DataContext is EditorViewModel editorViewModel)
        {
            editorViewModel.EditorModelChanged += EditorViewModel_EditorModelChanged;
            UpdateCurrentEditorMode(editorViewModel.CurrentEditorModel);
        }

        base.OnDataContextChanged(e);
    }

    private void EditorViewModel_EditorModelChanged(object? sender, EventArgs e)
    {
        UpdateCurrentEditorMode(ViewModel.CurrentEditorModel);
    }

    private void UpdateCurrentEditorMode(EditorModel editorModel)
    {
        if (editorModel.TextEditor is null)
        {
            TextEditor textEditor = CreateTextEditor(editorModel);
            editorModel.TextEditor = textEditor;
        }

        TextEditorBorder.Child = editorModel.TextEditor;
        CurrentTextEditor = editorModel.TextEditor;
    }

    private TextEditor CreateTextEditor(EditorModel editorModel)
    {
        TextEditor textEditor = new TextEditor();
        textEditor.TextEditorCore.SetExitDebugMode();

        textEditor.SetStyleTextRunProperty(runProperty => runProperty with
        {
            FontSize = 25,
            Foreground = new SolidColorSkiaTextBrush(SKColors.Azure)
        });

        textEditor.TextEditorCore.DocumentChanged += (sender, args) =>
        {
            UpdateEditorModel(textEditor, editorModel);
        };
        textEditor.TextEditorHandler = new SimpleWriteTextEditorHandler(textEditor);
        return textEditor;
    }

    private void UpdateEditorModel(TextEditor textEditor, EditorModel editorModel)
    {
        editorModel.SaveStatus = SaveStatus.Draft;

        if (editorModel.FileInfo is null)
        {
            // 此时标题未定
            var paragraphList = textEditor.ParagraphList;
            // 至少有两段的时候，才能按照第一段作为标题
            if (paragraphList.Count > 1)
            {
                var title = paragraphList[0].GetText();
                if (!string.IsNullOrWhiteSpace(title))
                {
                    var trimTitle = title.Trim();
                    if (trimTitle.Length > 5)
                    {
                        // 如果标题过长，截断，执行中间截断。规则是：
                        // 保留前 5 后 3 个字符，中间用省略号代替
                        trimTitle = trimTitle[..5] + "..." + trimTitle[^3..];
                    }
                    editorModel.Title = trimTitle;
                }
                else
                {
                    editorModel.Title = EditorModel.DefaultTitle;
                }
            }
            else
            {
                editorModel.Title = EditorModel.DefaultTitle;
            }
        }
    }

    public TextEditor CurrentTextEditor { get; private set; } = null!;
}

class SimpleWriteTextEditorHandler : TextEditorHandler
{
    public SimpleWriteTextEditorHandler(TextEditor textEditor) : base(textEditor)
    {
    }
}