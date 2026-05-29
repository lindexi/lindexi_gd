namespace MiniMaxSdk.Images.Models;

/// <summary>
/// 表示 MiniMax 文生图请求中的画风设置。
/// </summary>
/// <param name="StyleType">画风风格类型，可选值包括 <c>漫画</c>、<c>元气</c>、<c>中世纪</c>、<c>水彩</c>。</param>
/// <param name="StyleWeight">画风权重，取值范围为 <c>(0, 1]</c>，默认值为 <c>0.8</c>。</param>
public sealed record MiniMaxImageStyle(string StyleType, float? StyleWeight = null)
{
    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(StyleType))
        {
            throw new ArgumentException("画风类型不能为空。", nameof(StyleType));
        }

        if (StyleWeight.HasValue && (StyleWeight.Value <= 0 || StyleWeight.Value > 1))
        {
            throw new ArgumentOutOfRangeException(nameof(StyleWeight), StyleWeight, "画风权重需在 (0, 1] 区间内。");
        }
    }
}