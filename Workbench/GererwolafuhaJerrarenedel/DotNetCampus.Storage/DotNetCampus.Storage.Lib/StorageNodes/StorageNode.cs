using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace DotNetCampus.Storage.Lib.StorageNodes;

/// <summary>
/// 存储的数据最小单位。用于作为数据模型和具体文件存档格式的中间层
/// </summary>
/// 向下转换： 从 <see cref="StorageNode"/> 转换到具体的文件存档格式叫序列化，如 Json 序列化和 XML 序列化等，称为序列化
/// 向上转换： 从 <see cref="StorageNode"/> 转换到业务的数据模型，如 SaveInfo 等，称为 Parse 转换，使用 Parser 转换器进行转换
public class StorageNode
{
    public StorageNodeType StorageNodeType { get; set; }

    /// <summary>
    /// 名称
    /// </summary>
    public StorageTextSpan Name { get; set; }

    /// <summary>
    /// 值
    /// </summary>
    public StorageTextSpan Value { get; set; } = StorageTextSpan.NullValue;

    public List<StorageNode>? Children { get; set; }

    /// <summary>
    /// 是否拥有子节点
    /// </summary>
    [MemberNotNullWhen(true, nameof(Children))]
    public bool HasChild => Children != null && Children.Any();

    public override string ToString()
    {
        if (Children is null)
        {
            if (Value.IsNull)
            {
                return Name.ToString();
            }
            else
            {
                return $"{Name}: {Value}";
            }
        }
        else
        {
            Debug.Assert(Value.IsNull);
            return $"{Name} Count:{Children.Count}";
        }
    }

    internal StorageNode Clone()
    {
        var clone = new StorageNode
        {
            StorageNodeType = StorageNodeType,
            Name = Name,
            Value = Value,
            Children = Children?.Select(child => child.Clone()).ToList()
        };
        return clone;
    }
}