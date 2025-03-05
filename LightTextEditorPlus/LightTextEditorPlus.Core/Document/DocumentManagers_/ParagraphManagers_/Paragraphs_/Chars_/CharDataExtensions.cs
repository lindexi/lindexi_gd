namespace LightTextEditorPlus.Core.Document;

public static class CharDataExtensions
{
    public static CharInfo ToCharInfo(this CharData charData) => new CharInfo(charData.CharObject, charData.RunProperty);
}