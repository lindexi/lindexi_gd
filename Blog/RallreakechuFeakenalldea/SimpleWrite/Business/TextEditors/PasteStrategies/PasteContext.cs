using System;
using System.IO;

using Avalonia.Input.Platform;

using LightTextEditorPlus;

namespace SimpleWrite.Business.TextEditors.PasteStrategies;

/// <summary>
/// 粘贴上下文。为策略提供剪贴板访问、文档信息和文本插入能力。
/// </summary>
internal sealed class PasteContext
{
    private readonly TextEditor _textEditor;

    /// <summary>
    /// 初始化 <see cref="PasteContext"/> 的新实例。
    /// </summary>
    /// <param name="clipboard">剪贴板实例，用于读取剪贴板内容</param>
    /// <param name="documentFile">当前文档的文件信息，用于确定图片保存目录和文件名。为 null 表示文档未保存到本地</param>
    /// <param name="textEditor">文本编辑器实例，用于插入文本</param>
    public PasteContext(IClipboard clipboard, FileInfo? documentFile, TextEditor textEditor)
    {
        ArgumentNullException.ThrowIfNull(clipboard);
        ArgumentNullException.ThrowIfNull(textEditor);

        Clipboard = clipboard;
        DocumentFile = documentFile;
        _textEditor = textEditor;
    }

    /// <summary>
    /// 剪贴板实例，用于读取剪贴板内容。
    /// </summary>
    public IClipboard Clipboard { get; }

    /// <summary>
    /// 当前文档的文件信息，用于确定图片保存目录和文件名。为 null 表示文档未保存到本地。
    /// </summary>
    public FileInfo? DocumentFile { get; }

    /// <summary>
    /// 将文本插入到编辑器当前光标位置，替换当前选中的内容（如有）。
    /// </summary>
    /// <param name="text">要插入的文本</param>
    public void InsertText(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        _textEditor.EditAndReplace(text);
    }
}
