using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace LightTextEditorPlus;

// 发布命令： dotnet publish -r win-x86
// 自动进行 AOT 方式发布 Native Dll 文件

internal class TextEditor : SkiaTextEditor
{
    public TextEditor() : this(new NativeTextEditorPlatformProvider())
    {
    }

    private TextEditor(NativeTextEditorPlatformProvider provider) : base(provider)
    {
        _platformProvider = provider;
        _platformProvider.NativeTextEditor = this;
    }

    private readonly NativeTextEditorPlatformProvider _platformProvider;

    /// <summary>
    /// 创建文本编辑器
    /// </summary>
    /// <returns></returns>
    [UnmanagedCallersOnly(EntryPoint = "CreateTextEditor")]
    public static uint CreateTextEditor()
    {
        var textEditor = new TextEditor();
        uint id = Interlocked.Increment(ref _id);

        TextEditorDictionary[id] = textEditor;
        return id;
    }

    /// <summary>
    /// 释放文本编辑器
    /// </summary>
    /// <param name="textEditorId"></param>
    /// <returns></returns>
    [UnmanagedCallersOnly(EntryPoint = "FreeTextEditor")]
    public static int FreeTextEditor(uint textEditorId)
    {
        if (TextEditorDictionary.TryRemove(textEditorId, out _))
        {
            return ErrorCode.Success.Code;
        }
        else
        {
            return ErrorCode.TextEditorNotFound.Code;
        }
    }

    /// <summary>
    /// 设置文档宽度
    /// </summary>
    /// <param name="textEditorId"></param>
    /// <param name="documentWidth"></param>
    /// <returns></returns>
    [UnmanagedCallersOnly(EntryPoint = "SetDocumentWidth")]
    public static int SetDocumentWidth(uint textEditorId, double documentWidth)
    {
        if (!TextEditorDictionary.TryGetValue(textEditorId, out var textEditor))
        {
            if (textEditorId < _id)
            {
                return ErrorCode.TextEditorBeFree;
            }

            return ErrorCode.TextEditorNotFound;
        }

        textEditor.TextEditorCore.DocumentManager.DocumentWidth = documentWidth;
        return ErrorCode.Success;
    }

    /// <summary>
    /// 设置文档高度
    /// </summary>
    /// <param name="textEditorId"></param>
    /// <param name="documentHeight"></param>
    /// <returns></returns>
    [UnmanagedCallersOnly(EntryPoint = "SetDocumentHeight")]
    public static int SetDocumentHeight(uint textEditorId, double documentHeight)
    {
        if (!TextEditorDictionary.TryGetValue(textEditorId, out var textEditor))
        {
            if (textEditorId < _id)
            {
                return ErrorCode.TextEditorBeFree;
            }

            return ErrorCode.TextEditorNotFound;
        }

        textEditor.TextEditorCore.DocumentManager.DocumentHeight = documentHeight;
        return ErrorCode.Success;
    }

    /// <summary>
    /// 追加文本到指定的文本编辑器中
    /// </summary>
    /// <param name="textEditorId"></param>
    /// <param name="unicode16Text">采用 unicode 16 编码的字符串</param>
    /// <param name="charCount">字符串包含的字符数量。是字符数量，而不是 byte 数量</param>
    [UnmanagedCallersOnly(EntryPoint = "AppendText")]
    public static int AppendText(uint textEditorId, IntPtr unicode16Text, int charCount)
    {
        if (!TextEditorDictionary.TryGetValue(textEditorId, out var textEditor))
        {
            if (textEditorId < _id)
            {
                return ErrorCode.TextEditorBeFree;
            }

            return ErrorCode.TextEditorNotFound;
        }

        string text = Marshal.PtrToStringUni(unicode16Text, charCount);
        textEditor.AppendText(text);

        return ErrorCode.Success;
    }

    /// <summary>
    /// 将文本编辑器的内容保存为图片文件
    /// </summary>
    /// <param name="textEditorId"></param>
    /// <param name="unicode16FilePath"></param>
    /// <param name="charCountOfFilePath"></param>
    /// <returns></returns>
    [UnmanagedCallersOnly(EntryPoint = "SaveAsImageFile")]
    public static int SaveAsImageFile(uint textEditorId, IntPtr unicode16FilePath, int charCountOfFilePath)
    {
        if (!TextEditorDictionary.TryGetValue(textEditorId, out var textEditor))
        {
            return ErrorCode.TextEditorNotFound;
        }

        string filePath = Marshal.PtrToStringUni(unicode16FilePath, charCountOfFilePath);

        filePath = Path.GetFullPath(filePath);

        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        textEditor.SaveAsImageFile(filePath);

        Console.WriteLine($"将文本框画面保存到图片文件");

        return ErrorCode.Success;
    }

    private static uint _id = 0;

    private static readonly ConcurrentDictionary<uint/*Id*/, TextEditor> TextEditorDictionary = new ConcurrentDictionary<uint, TextEditor>();
}

class NativeTextEditorPlatformProvider : SkiaTextEditorPlatformProvider
{
    public TextEditor NativeTextEditor { get; set; } = null!;
}