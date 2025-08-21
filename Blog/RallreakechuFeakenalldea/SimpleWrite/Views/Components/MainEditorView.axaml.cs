using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

using LightTextEditorPlus;
using LightTextEditorPlus.Primitive;

using SimpleWrite.ViewModels;

using SkiaSharp;

using System;
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
            TextEditor textEditor = new TextEditor();
            textEditor.TextEditorCore.SetExitDebugMode();

            textEditor.SetStyleTextRunProperty(runProperty => runProperty with
            {
                FontSize = 25,
                Foreground = new SolidColorSkiaTextBrush(SKColors.Azure)
            });

            editorModel.TextEditor = textEditor;

            textEditor.TextEditorCore.DocumentChanged += (sender, args) =>
            {
                UpdateEditorModel(textEditor, editorModel);
            };
        }

        TextEditorBorder.Child = editorModel.TextEditor;
        CurrentTextEditor = editorModel.TextEditor;
    }

    private void UpdateEditorModel(TextEditor textEditor, EditorModel editorModel)
    {
        editorModel.SaveStatus = SaveStatus.Draft;

        if (editorModel.FileInfo is null)
        {
            // 此时标题未定
            var paragraphList = textEditor.ParagraphList;
            if (paragraphList.Count > 0)
            {
                var title = paragraphList[0].GetText();
                if (title.Length > 5)
                {
                    // 如果标题过长，截断，执行中间截断。规则是：
                    // 保留前 5 后 3 个字符，中间用省略号代替
                    title = title[..5] + "..." + title[^3..];
                }
                editorModel.Title = title;
            }
            else
            {
                editorModel.Title = "无标题";
            }
        }
    }

    public TextEditor CurrentTextEditor { get; private set; } = null!;
}

