using System.Collections.Generic;
using System.IO;

using Avalonia.Media;

namespace LightTextEditorPlus.FontManagers;

public class TextEditorFontResourceManager
{
    private static void RegisterFontNameToResource(string fontName, FontFamily fontFamily)
    {
        // 由于 Avalonia 的 FontFamily 无法直接转换为 SKTypeface 不给开放，因此这个方法先不开出来
        ResourceDictionary[fontName] = fontFamily;
    }

    /// <summary>
    /// 尝试注册字体名到资源
    /// </summary>
    /// <param name="fontName"></param>
    /// <param name="fontFile"></param>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException"></exception>
    public static bool TryRegisterFontNameToResource(string fontName, FileInfo fontFile)
    {
        if (FontFileDictionary.TryAdd(fontName, fontFile))
        {
            if (!fontFile.Exists)
            {
                fontFile.Refresh();
                if (!fontFile.Exists)
                {
                    throw new FileNotFoundException($"Font file not found: {fontFile.FullName}");
                }
            }

            return true;
        }
        else
        {
            // 都注册失败了，还管文件是否存在干啥
            return false;
        }
    }

    internal static Dictionary<string/*fontName*/, FileInfo/*fontFile*/> FontFileDictionary { get; } = new();

    internal static Dictionary<string /*fontName*/, FontFamily> ResourceDictionary { get; } = new();
}
