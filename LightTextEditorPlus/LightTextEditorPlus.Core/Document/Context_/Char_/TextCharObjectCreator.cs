namespace LightTextEditorPlus.Core.Document;

static class TextCharObjectCreator
{
    public static ICharObject CreateCharObject(string text, int charIndex, int charCount = 1)
    {
        if (charCount == 1)
        {
            return new SingleCharObject(text[charIndex]);
        }
        else
        {
            return new TextSpanCharObject(text, charIndex, charCount);
        }
    }
}