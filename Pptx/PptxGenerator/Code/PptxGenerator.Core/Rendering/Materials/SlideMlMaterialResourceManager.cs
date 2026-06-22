namespace PptxGenerator.Rendering.Materials;

/// <summary>
/// 管理 SlideML 渲染过程中使用的素材资源，支持按键存取、增删改查及批量替换。
/// </summary>
public class SlideMlMaterialResourceManager
{
    /// <summary>
    /// 尝试获取指定键的素材。
    /// </summary>
    /// <typeparam name="T">素材类型。</typeparam>
    /// <param name="key">素材键。</param>
    /// <param name="material">当返回 <see langword="true"/> 时，包含获取到的素材。</param>
    /// <returns>如果找到且类型匹配，返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    public bool TryGetMaterial<T>(string key, out T material) where T : ISlideMlMaterial
    {
        if (_materialDictionary.TryGetValue(key, out var mat) && mat is T tMat)
        {
            material = tMat;
            return true;
        }
        material = default!;
        return false;
    }

    /// <summary>
    /// 添加素材。如果已存在相同键的素材，则抛出异常。
    /// </summary>
    /// <param name="key">素材键。</param>
    /// <param name="material">要添加的素材。</param>
    /// <exception cref="ArgumentNullException"><paramref name="material"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="ArgumentException"><paramref name="key"/> 为 <see langword="null"/> 或空白，或已存在相同键的素材。</exception>
    public void AddMaterial(string key, ISlideMlMaterial material) 
    {
        ArgumentNullException.ThrowIfNull(material);
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));
        }
        _materialDictionary.Add(key, material);
    }

    /// <summary>
    /// 设置素材。如果已存在相同键的素材，则覆盖。
    /// </summary>
    /// <param name="key">素材键。</param>
    /// <param name="material">要设置的素材。</param>
    /// <exception cref="ArgumentNullException"><paramref name="material"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="ArgumentException"><paramref name="key"/> 为 <see langword="null"/> 或空白。</exception>
    public void SetMaterial(string key, ISlideMlMaterial material)
    {
        ArgumentNullException.ThrowIfNull(material);
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));
        }
        _materialDictionary[key] = material;
    }

    /// <summary>
    /// 删除指定键的素材。
    /// </summary>
    /// <param name="key">要删除的素材键。</param>
    /// <returns>如果成功删除，返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    /// <exception cref="ArgumentException"><paramref name="key"/> 为 <see langword="null"/> 或空白。</exception>
    public bool RemoveMaterial(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));
        }
        return _materialDictionary.Remove(key);
    }

    /// <summary>
    /// 清空所有素材。
    /// </summary>
    public void ClearMaterials()
    {
        _materialDictionary.Clear();
    }

    /// <summary>
    /// 批量替换素材。清空现有素材后，添加指定的素材集合。
    /// </summary>
    /// <param name="materials">要替换的素材字典。</param>
    /// <exception cref="ArgumentNullException"><paramref name="materials"/> 为 <see langword="null"/>。</exception>
    /// <exception cref="ArgumentException">素材字典中包含 <see langword="null"/> 或空白的键，或值为 <see langword="null"/>。</exception>
    public void ReplaceMaterials(IReadOnlyDictionary<string, ISlideMlMaterial> materials)
    {
        ArgumentNullException.ThrowIfNull(materials);
        _materialDictionary.Clear();
        foreach (var (key, material) in materials)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Key cannot be null or whitespace.", nameof(materials));
            }
            ArgumentNullException.ThrowIfNull(material);
            _materialDictionary.Add(key, material);
        }
    }

    /// <summary>
    /// 获取所有素材
    /// </summary>
    /// <returns></returns>
    public IReadOnlyList<string> GetAllMaterialKey()
    {
        return _materialDictionary.Keys.ToList();
    }

    private readonly Dictionary<string, ISlideMlMaterial> _materialDictionary = new Dictionary<string, ISlideMlMaterial>();
}
