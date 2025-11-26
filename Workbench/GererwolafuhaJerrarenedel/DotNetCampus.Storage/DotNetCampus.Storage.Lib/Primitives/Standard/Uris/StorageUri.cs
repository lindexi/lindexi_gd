using System;
using System.Buffers.Text;

using Uri = DotNetCampus.Storage.Standard.StorageUri;
using UriContext = DotNetCampus.Storage.Standard.StorageUriContext;

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
    public static Uri? Create(string value)
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

        //if (value.StartsWith(StorageUriContext.AppPrefix, StringComparison.OrdinalIgnoreCase))
        //{
        //    return new AppUri(value);
        //}

        //if (value.StartsWith(StorageUriContext.Base64Prefix, StringComparison.OrdinalIgnoreCase))
        //{
        //    return new Base64Uri(value);
        //}

        return null;
    }
}

internal static class UriUtils
{
    /// <summary>
    /// 去掉值的前缀
    /// </summary>
    /// <param name="value"></param>
    /// <param name="prefix"></param>
    /// <returns></returns>
    public static string RemovePrefix(string value, string prefix)
    {
        // 下面代码解决 value = " id://foo" 的问题
        value = value.Trim();
        // 前缀是内部传入的，确保不需要删除空格
        //prefix = prefix.Trim();

        // 忽略大小写 协议应该都不会区分大小写
        if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return value.Substring(prefix.Length).Trim();
        }

        return value;
    }
}

/// <summary>
/// 用于存储的文件链接
/// </summary>
public class FileUri : Uri
{
    /// <summary>
    /// 创建用于存储的文件链接
    /// </summary>
    /// <param name="value"></param>
    public FileUri(string value)
    {
        Value = UriUtils.RemovePrefix(value, UriContext.FilePrefix);
    }

    public override string Value { get; }

    /// <inheritdoc />
    public override string Encode()
    {
        return $"{UriContext.FilePrefix}{Value}";
    }
}

/// <summary>
/// 用于存储的 Http 链接
/// </summary>
public class HttpUri : Uri
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

///// <summary>
///// 用于存储的 App 链接
///// </summary>
//public class AppUri : Uri
//{
//    /// <summary>
//    /// 创建用于存储的 App 链接
//    /// </summary>
//    /// <param name="value"></param>
//    public AppUri(string value)
//    {
//        Value = UriUtils.RemovePrefix(value, UriContext.AppPrefix);
//    }

//    public override string Value { get; }

//    /// <inheritdoc />
//    public override string Encode()
//    {
//        return $"{UriContext.AppPrefix}{Value}";
//    }
//}

/// <summary>
/// 用于存储的 Id 链接
/// </summary>
public class IdUri : Uri
{
    /// <summary>
    /// 创建用于存储的 Id 链接
    /// </summary>
    public IdUri(string value)
    {
        Value = value.Replace(UriContext.IdPrefix, "");
    }

    public override string Value { get; }

    /// <inheritdoc />
    public override string Encode()
    {
        return $"{UriContext.IdPrefix}{Value}";
    }
}

///// <summary>
///// 用于存储的 Base64 链接
///// </summary>
//public class Base64Uri : Uri
//{
//    /// <summary>
//    /// 创建用于存储的 Base64 链接
//    /// </summary>
//    /// <param name="value"></param>
//    public Base64Uri(string value)
//    {
//        Value = value;
//    }

//    public override string Value { get; }

//    /// <inheritdoc />
//    public override string Encode()
//    {
//        return Value;
//    }
//}