namespace DotNetCampus.Storage.Standard;

/// <summary>
/// 用于存储的 Id 链接
/// </summary>
public class IdUri : StorageUri
{
    /// <summary>
    /// 创建用于存储的 Id 链接
    /// </summary>
    public IdUri(string value)
    {
        Value = value.Replace(StorageUriContext.IdPrefix, "");
    }

    public override string Value { get; }

    /// <inheritdoc />
    public override string Encode()
    {
        return $"{StorageUriContext.IdPrefix}{Value}";
    }
}