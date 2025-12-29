namespace DotNetCampus.Storage.Standard;

/// <summary>
/// 用于记录 Uri 用到的常量
/// </summary>
public static class StorageUriContext
{
    /// <summary>
    /// Base64链接前缀
    /// </summary>
    public const string Base64Prefix = "base64://";

    /// <summary>
    /// App链接前缀
    /// </summary>
    public const string AppPrefix = "app://";

    /// <summary>
    /// 文件链接前缀
    /// </summary>
    public const string FilePrefix = "file://";

    /// <summary>
    /// Id链接前缀
    /// </summary>
    public const string IdPrefix = "id://";

    /// <summary>
    /// Http链接前缀
    /// </summary>
    public const string HttpPrefix = "http://";

    /// <summary>
    /// Https链接前缀
    /// </summary>
    public const string HttpsPrefix = "https://";
}