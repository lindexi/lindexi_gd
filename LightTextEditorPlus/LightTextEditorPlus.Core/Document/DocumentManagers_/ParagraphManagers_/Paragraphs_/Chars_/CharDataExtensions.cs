namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 对 <see cref="CharData"/> 的扩展
/// </summary>
public static class CharDataExtensions
{
    /// <summary>
    /// 转换为 <see cref="CharInfo"/> 结构体
    /// </summary>
    /// <param name="charData"></param>
    /// <returns></returns>
    public static CharInfo ToCharInfo(this CharData charData) => new CharInfo(charData.CharObject, charData.RunProperty);
}