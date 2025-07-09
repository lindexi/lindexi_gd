using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCampus.Storage.Lib;

/// <summary>
/// 存储的数据最小单位。用于作为数据模型和具体文件存档格式的中间层
/// </summary>
public class StorageNode
{
    public StorageNodeType StorageNodeType { get; set; }

    public StorageTextSpan Name { get; set; }

    public StorageTextSpan Value { get; set; } = StorageTextSpan.NullValue;

    public List<StorageNode>? Children { get; set; }

}