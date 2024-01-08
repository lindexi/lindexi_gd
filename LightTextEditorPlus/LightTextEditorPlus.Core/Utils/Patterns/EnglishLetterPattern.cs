namespace LightTextEditorPlus.Core.Utils.Patterns;

class EnglishLetterPattern : IPattern
{
    public bool IsInRange(char c)
    {
        return char.IsLower(c) || char.IsUpper(c);
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