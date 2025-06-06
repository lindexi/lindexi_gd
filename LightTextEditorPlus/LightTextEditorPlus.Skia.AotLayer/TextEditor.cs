using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace LightTextEditorPlus;

internal static class TextEditor
{
    [UnmanagedCallersOnly(EntryPoint = "CreateTextEditor")]
    public static uint CreateTextEditor()
    {
        var skiaTextEditor = new SkiaTextEditor();
        uint id = Interlocked.Increment(ref _id);

        Console.WriteLine($"SkiaTextEditor={id}");

        SkiaTextEditorDictionary[id] = skiaTextEditor;
        return id;
    }

    [UnmanagedCallersOnly(EntryPoint = "FreeTextEditor")]
    public static int FreeTextEditor(uint textEditorId)
    {
        if (SkiaTextEditorDictionary.TryRemove(textEditorId, out _))
        {
            return ErrorCode.Success.Code;
        }
        else
        {
            return ErrorCode.TextEditorNotFound.Code;
        }
    }

    /// <summary>
    /// 追加文本到指定的文本编辑器中
    /// </summary>
    /// <param name="textEditorId"></param>
    /// <param name="unicode16Text">采用 unicode 16 编码的字符串</param>
    /// <param name="charCount">字符串包含的字符数量。是字符数量，而不是 byte 数量</param>
    [UnmanagedCallersOnly(EntryPoint = "AppendText")]
    public static unsafe int AppendText(uint textEditorId, IntPtr unicode16Text, int charCount)
    {
        if (!SkiaTextEditorDictionary.TryGetValue(textEditorId, out var textEditor))
        {
            if (textEditorId < _id)
            {
                return ErrorCode.TextEditorBeFree.Code;
            }

            return ErrorCode.TextEditorNotFound.Code;
        }

        string text = Marshal.PtrToStringUni(unicode16Text, charCount);
        textEditor.AppendText(text);

        return ErrorCode.Success.Code;
    }

    [UnmanagedCallersOnly(EntryPoint = "SaveAsImageFile")]
    public static int SaveAsImageFile(uint textEditorId, IntPtr unicode16FilePath, int charCountOfFilePath)
    {
        if (!SkiaTextEditorDictionary.TryGetValue(textEditorId, out var textEditor))
        {
            return ErrorCode.TextEditorNotFound.Code;
        }

        string filePath = Marshal.PtrToStringUni(unicode16FilePath, charCountOfFilePath);

        filePath = Path.GetFullPath(filePath);

        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        textEditor.SaveAsImageFile(filePath);

        Console.WriteLine($"将文本框画面保存到图片文件");

        return ErrorCode.Success.Code;
    }

    private static uint _id = 0;

    private static readonly ConcurrentDictionary<uint/*Id*/, SkiaTextEditor> SkiaTextEditorDictionary = new ConcurrentDictionary<uint, SkiaTextEditor>();
}