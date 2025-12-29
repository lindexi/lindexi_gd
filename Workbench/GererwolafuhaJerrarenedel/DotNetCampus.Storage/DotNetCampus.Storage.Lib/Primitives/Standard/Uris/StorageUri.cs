using System;
using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;

namespace DotNetCampus.Storage.Standard;

/// <summary>
/// 用于存储的链接
/// </summary>
public abstract class StorageUri
{
    /// <summary>
    /// 链接的值，如想要返回 Uri 链接，请调用 <see cref="Encode"/> 方法
    /// </summary>
    public abstract string Value { get; }

    /// <summary>
    /// 返回编码后的值，如文件链接的 Value 是文件的绝对值，而 <see cref="Encode"/> 返回的是 file://文件地址 的字符串
    /// </summary>
    /// <returns></returns>
    public abstract string Encode();

    public static implicit operator StorageUri?(System.Uri? value)
    {
        if (value == null)
        {
            return null;
        }

        var uri = Create(value.OriginalString);
        if (uri != null)
        {
            return uri;
        }

        return new FileUri(value.OriginalString);
    }

    [return: NotNullIfNotNull(nameof(uri))]
    public static implicit operator System.Uri?(StorageUri? uri)
    {
        if (uri == null)
        {
            return null;
        }

        return new System.Uri(uri.Value, UriKind.RelativeOrAbsolute);
    }

    /// <summary>
    /// 通过传入的字符串创建链接
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static StorageUri? Create(string value)
    {
        if (value.StartsWith(StorageUriContext.FilePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return new FileUri(value);
        }

        if (value.StartsWith(StorageUriContext.IdPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return new IdUri(value);
        }

        if (value.StartsWith(StorageUriContext.HttpPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return new HttpUri(value);
        }

        if (value.StartsWith(StorageUriContext.HttpsPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return new HttpUri(value);
        }

        if (value.StartsWith(StorageUriContext.AppPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return new AppUri(value);
        }

        if (value.StartsWith(StorageUriContext.Base64Prefix, StringComparison.OrdinalIgnoreCase))
        {
            return new Base64Uri(value);
        }

        return null;
    }
}

/// <summary>
/// 用于存储的 App 链接
/// </summary>
file class AppUri : StorageUri
{
    /// <summary>
    /// 创建用于存储的 App 链接
    /// </summary>
    /// <param name="value"></param>
    public AppUri(string value)
    {
        Value = UriUtils.RemovePrefix(value, StorageUriContext.AppPrefix);
    }

    public override string Value { get; }

    /// <inheritdoc />
    public override string Encode()
    {
        return $"{StorageUriContext.AppPrefix}{Value}";
    }
}

/// <summary>
/// 用于存储的 Base64 链接
/// </summary>
file class Base64Uri : StorageUri
{
    /// <summary>
    /// 创建用于存储的 Base64 链接
    /// </summary>
    /// <param name="value"></param>
    public Base64Uri(string value)
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