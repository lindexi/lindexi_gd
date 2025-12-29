namespace DotNetCampus.Storage.Standard;

/// <summary>
/// 用于存储的 Http 链接
/// </summary>
public class HttpUri : StorageUri
{
    /// <summary>
    /// 创建用于存储的 Http 链接
    /// </summary>
    /// <param name="value"></param>
    public HttpUri(string value)
    {
        Value = value;
    }

    public override string Value { get; }

    /// <inheritdoc />
    public override string Encode()
    {
        return Value;
    }
}