namespace LightTextEditorPlus.Core.Utils.Patterns;

class LetterPattern : IPattern
{
    public bool IsInRange(char c)
    {
        return char.IsLetter(c);
    }

    public bool ContainInRange(string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            if (char.IsLetter(text, i))
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
            if (!char.IsLetter(text, i))
            {
                return false;
            }
        }

        return true;
    }
}