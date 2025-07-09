using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCampus.Storage.Lib;

public class StorageNode
{
    public StorageNodeType StorageNodeType { get; set; }
}

/// <summary>
/// 存储类型
/// </summary>
public enum StorageNodeType
{
    /// <summary>
    /// 属性
    /// </summary>
    Property,

    /// <summary>
    /// 元素，独立的对象
    /// </summary>
    Element,

    /// <summary>
    /// 列表
    /// </summary>
    List,
}