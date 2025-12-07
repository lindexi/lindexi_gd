using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Media;

using LightTextEditorPlus;
using LightTextEditorPlus.Editing;
using LightTextEditorPlus.Primitive;

using SimpleWrite.Business;
using SimpleWrite.Business.ShortcutManagers;
using SimpleWrite.Models;
using SimpleWrite.ViewModels;

using SkiaSharp;

using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Interactivity;
using SimpleWrite.Business.FileHandlers;

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

    protected override void OnLoaded(RoutedEventArgs e)
    {
        // 提供 View 层的功能给 ViewModel 使用
        if (TopLevel.GetTopLevel(this) is {} topLevel)
        {
            ViewModel.SaveFilePickerHandler ??= new SaveFilePickerHandler(topLevel);
        }

        base.OnLoaded(e);
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

            // 如果此时已经有文件了，就加载文件内容
            if (editorModel.FileInfo is { } fileInfo)
            {
                _ = ViewModel.LoadFileToTextEditorAsync(editorModel, textEditor,fileInfo);
            }
        }

        TextEditorBorder.Child = editorModel.TextEditor;
        CurrentTextEditor = editorModel.TextEditor;
    }

    /// <summary>
    /// 创建文本编辑器
    /// </summary>
    /// <param name="editorModel"></param>
    /// <returns></returns>
    private SimpleWriteTextEditor CreateTextEditor(EditorModel editorModel)
    {
        var textEditor = new SimpleWriteTextEditor();
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
       
        textEditor.TextEditorHandler = new SimpleWriteTextEditorHandler(textEditor)
        {
            ShortcutExecutor = ShortcutExecutor,
        };
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

    private ShortcutExecutor ShortcutExecutor => _shortcutExecutor ??= new ShortcutExecutor()
    {
        ShortcutManager = ViewModel.ShortcutManager
    };
    private ShortcutExecutor? _shortcutExecutor;

    public TextEditor CurrentTextEditor { get; private set; } = null!;
}

class SimpleWriteTextEditorHandler : TextEditorHandler
{
    public SimpleWriteTextEditorHandler(TextEditor textEditor) : base(textEditor)
    {
    }

    public required ShortcutExecutor ShortcutExecutor { get; init; }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        // 判断是否落在快捷键范围内
        var shortcutHandled = ShortcutExecutor.Handle(e);
        if (shortcutHandled)
        {
            // 被快捷键处理了，就不继续往下传递
            return;
        }

        base.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
    }
}