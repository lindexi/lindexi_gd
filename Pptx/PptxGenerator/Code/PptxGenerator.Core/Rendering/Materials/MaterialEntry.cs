namespace PptxGenerator.Rendering.Materials;

/// <summary>
/// 素材条目，承载素材键与素材值的只读结构体，用于批量设置素材。
/// </summary>
/// <param name="Key">素材键。</param>
/// <param name="Material">素材实例。</param>
public readonly record struct MaterialEntry(string Key, ISlideMlMaterial Material);
