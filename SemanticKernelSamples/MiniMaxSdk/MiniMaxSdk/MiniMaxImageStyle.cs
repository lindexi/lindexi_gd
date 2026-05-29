namespace MiniMaxSdk;

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