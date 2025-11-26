using System.Diagnostics.CodeAnalysis;

namespace DotNetCampus.Storage.Standard;

/// <summary>
/// 用于存储的文件链接
/// </summary>
public class FileUri : StorageUri
{
    /// <summary>
    /// 创建用于存储的文件链接
    /// </summary>
    /// <param name="value"></param>
    public FileUri(string value)
    {
        Value = UriUtils.RemovePrefix(value, StorageUriContext.FilePrefix);
    }

    public override string Value { get; }

    /// <inheritdoc />
    public override string Encode()
    {
        return $"{StorageUriContext.FilePrefix}{Value}";
    }

    [return: NotNullIfNotNull(nameof(value))]
    public static implicit operator FileUri?(FileInfo? value)
    {
        if (value is null)
        {
            return null;
        }

        return new FileUri(value.FullName);
    }
}