namespace LightTextEditorPlus.Core.Document;

public class TextRun : ITextRun
{
    public TextRun(string text, IReadOnlyRunProperty? runProperty=null)
    {
        Text = text;
        RunProperty = runProperty;
    }

    public string Text { get; }

    public int Count => Text.Length;

    public ICharObject GetChar(int index)
    {
        return new TextCharObject(Text[index].ToString());
    }

    public IReadOnlyRunProperty? RunProperty { get; }
}