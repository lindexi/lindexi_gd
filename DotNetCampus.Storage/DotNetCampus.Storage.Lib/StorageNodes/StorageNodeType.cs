namespace DotNetCampus.Storage.StorageNodes;

/// <summary>
/// 存储类型
/// </summary>
public enum StorageNodeType
{
    /// <summary>
    /// 未知
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// 属性
    /// </summary>
    Property,

    /// <summary>
    /// 元素，独立的对象
    /// </summary>
    Element,
}