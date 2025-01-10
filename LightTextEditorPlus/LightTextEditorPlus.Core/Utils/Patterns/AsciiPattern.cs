namespace LightTextEditorPlus.Core.Utils.Patterns;

class AsciiPattern : IPattern
{
    public bool IsInRange(char c)
    {
        return char.IsAscii(c);
    }

    public bool ContainInRange(string text)
    {
        foreach (var c in text)
        {
            if (char.IsAscii(c))
            {
                return true;
            }
        }

        return false;
    }

    public bool AreAllInRange(string text)
    {
        foreach (var c in text)
        {
            if (!char.IsAscii(c))
            {
                return false;
            }
        }

        return true;
    }
}