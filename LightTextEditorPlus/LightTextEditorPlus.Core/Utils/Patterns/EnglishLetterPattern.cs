using System.Text;
using LightTextEditorPlus.Core.Primitive;

namespace LightTextEditorPlus.Core.Utils.Patterns;

class EnglishLetterPattern : IPattern
{
    public bool IsInRange(char c)
    {
        return char.IsLower(c) || char.IsUpper(c);
    }

    public bool IsInRange(Utf32CodePoint codePoint)
    {
        // 不能使用 IsLetter 因为会将中文等字符也包含进去
        //return Rune.IsLetter(codePoint.Rune);
        Rune rune = codePoint.Rune;
        return Rune.IsLower(rune) || Rune.IsUpper(rune);
    }

    public bool ContainInRange(string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            // .NET 7 char.IsAsciiLetter
            if (char.IsLower(text, i) || char.IsUpper(text, i))
            {
                return true;
            }
        }

        return false;
    }

    public bool AreAllInRange(string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            if (!(char.IsLower(text, i) || char.IsUpper(text, i)))
            {
                return false;
            }
        }

        return true;
    }
}
