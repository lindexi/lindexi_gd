namespace MiniMaxSdk.Images.Models;

/// <summary>
/// MiniMax 图生图支持的主体参考类型。
/// </summary>
public static class MiniMaxImageSubjectReferenceTypes
{
    /// <summary>
    /// 人物主体参考。
    /// </summary>
    public const string Character = "character";

    /// <summary>
    /// 判断指定主体参考类型是否受支持。
    /// </summary>
    /// <param name="type">待判断的主体参考类型。</param>
    /// <returns>如果受支持则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    public static bool IsSupported(string type) => type is Character;
}