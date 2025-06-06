using System.Runtime.InteropServices;

using static System.Net.Mime.MediaTypeNames;

// ReSharper disable once CheckNamespace
namespace LightTextEditorPlus;

public class TextEditorWrapper
{
    public const string DllName = "LightTextEditorPlus.dll";

    [DllImport(DllName)]
    public static extern TextEditorId CreateTextEditor();

    [DllImport(DllName)]
    public static extern int FreeTextEditor(TextEditorId textEditorId);

    [DllImport(DllName)]
    public static extern int AppendText(TextEditorId textEditorId, IntPtr unicode16Text, int charCount);

    public static unsafe int AppendText(TextEditorId textEditorId, string text)
    {
        fixed (char* p = text)
        {
            return AppendText(textEditorId, new IntPtr(p), text.Length);
        }
    }

    [DllImport(DllName)]
    public static extern int SaveAsImageFile
        (TextEditorId textEditorId, IntPtr unicode16FilePath, int charCountOfFilePath);

    public static unsafe int SaveAsImageFile(TextEditorId textEditorId, string filePath)
    {
        fixed (char* p = filePath)
        {
            return SaveAsImageFile(textEditorId, new IntPtr(p), filePath.Length);
        }
    }
}