using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using LightTextEditorPlus.Platform;

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
    public static int CreateTextEditor()
    {
        // 为什么不采用 GCHandler 返回 Handler 呢？因为不确定业务层会持有多久，不合适直接返回指针，防止长时间无法被 GC 掉
        var textEditor = new TextEditor();
        return TextEditorStore.Create(textEditor);
    }

    /// <summary>
    /// 释放文本编辑器
    /// </summary>
    /// <param name="textEditorId"></param>
    /// <returns></returns>
    [UnmanagedCallersOnly(EntryPoint = "FreeTextEditor")]
    public static int FreeTextEditor(int textEditorId)
    {
        return TextEditorStore.TryRemove(textEditorId, out var errorCode)
            ? ErrorCode.Success
            : errorCode;
    }

    /// <summary>
    /// 设置文档宽度
    /// </summary>
    /// <param name="textEditorId"></param>
    /// <param name="documentWidth"></param>
    /// <returns></returns>
    [UnmanagedCallersOnly(EntryPoint = "SetDocumentWidth")]
    public static int SetDocumentWidth(int textEditorId, double documentWidth)
    {
        if (!TryGetEditor(textEditorId, out var textEditor, out var errorCode))
        {
            return errorCode;
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
    public static int SetDocumentHeight(int textEditorId, double documentHeight)
    {
        if (!TryGetEditor(textEditorId, out var textEditor, out var errorCode))
        {
            return errorCode;
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
    public static int AppendText(int textEditorId, IntPtr unicode16Text, int charCount)
    {
        if (!TryGetEditor(textEditorId, out var textEditor,out var errorCode))
        {
            return errorCode;
        }

        string text = Marshal.PtrToStringUni(unicode16Text, charCount);
        textEditor.AppendText(text);

        return ErrorCode.Success;
    }

    internal static bool TryGetEditor(int textEditorId, [NotNullWhen(true)] out TextEditor? textEditor, out ErrorCode errorCode)
    {
        return TextEditorStore.TryGet(textEditorId, out textEditor, out errorCode);
    }

    /// <summary>
    /// 将文本编辑器的内容保存为图片文件
    /// </summary>
    /// <param name="textEditorId"></param>
    /// <param name="unicode16FilePath"></param>
    /// <param name="charCountOfFilePath"></param>
    /// <returns></returns>
    [UnmanagedCallersOnly(EntryPoint = "SaveAsImageFile")]
    public static int SaveAsImageFile(int textEditorId, IntPtr unicode16FilePath, int charCountOfFilePath)
    {
        if (!TryGetEditor(textEditorId, out var textEditor, out var errorCode))
        {
            return errorCode;
        }

        string filePath = Marshal.PtrToStringUni(unicode16FilePath, charCountOfFilePath);

        filePath = Path.GetFullPath(filePath);

        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        textEditor.SaveAsImageFile(filePath);

        Console.WriteLine($"将文本框画面保存到图片文件");

        return ErrorCode.Success;
    }

    private static readonly NativeObjectStore<TextEditor> TextEditorStore =
        new(ErrorCode.TextEditorNotFound, ErrorCode.TextEditorBeFree);
}

class NativeTextEditorPlatformProvider : SkiaTextEditorPlatformProvider
{
    public TextEditor NativeTextEditor { get; set; } = null!;
}