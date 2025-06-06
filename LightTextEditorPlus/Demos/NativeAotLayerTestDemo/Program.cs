// See https://aka.ms/new-console-template for more information

using System.Runtime.InteropServices;
using System.Text;
using LightTextEditorPlus;

if (Environment.Is64BitProcess)
{
    Console.WriteLine($"当前引用的是 x86 版本，不能使用 x64 方式运行");
}

string text = "这是一段文本";

unsafe
{
    fixed (char* t = text)
    {
        IntPtr p = new IntPtr(t);

        int byteCount = Encoding.Unicode.GetByteCount(text);

        // 实际测试证明传入的是字符数量，而不是 byte 数量
        string message = Marshal.PtrToStringUni(p, byteCount);
    }
}

TextEditorId textEditorId = TextEditorWrapper.CreateTextEditor();
Console.WriteLine(textEditorId);

var errorCode = TextEditorWrapper.AppendText(textEditorId, text);

errorCode = TextEditorWrapper.SaveAsImageFile(textEditorId, "1.png");

TextEditorWrapper.FreeTextEditor(textEditorId);

/*
System.DllNotFoundException:“Unable to load DLL 'CreateTextEditor' or one of its dependencies: 找不到指定的模块。 (0x8007007E)”
*/
Console.WriteLine("Hello, World!");