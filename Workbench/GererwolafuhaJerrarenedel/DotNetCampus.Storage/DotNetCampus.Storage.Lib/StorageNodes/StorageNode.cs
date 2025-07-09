using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DotNetCampus.Storage.Lib;

/// <summary>
/// 存储的数据最小单位。用于作为数据模型和具体文件存档格式的中间层
/// </summary>
/// 向下转换： 从 <see cref="StorageNode"/> 转换到具体的文件存档格式叫序列化，如 Json 序列化和 XML 序列化等，称为序列化
/// 向上转换： 从 <see cref="StorageNode"/> 转换到业务的数据模型，如 SaveInfo 等，称为 Parse 转换，使用 Parser 转换器进行转换
public class StorageNode
{
    public StorageNodeType StorageNodeType { get; set; }

    public StorageTextSpan Name { get; set; }

    public StorageTextSpan Value { get; set; } = StorageTextSpan.NullValue;

    public List<StorageNode>? Children { get; set; }
}