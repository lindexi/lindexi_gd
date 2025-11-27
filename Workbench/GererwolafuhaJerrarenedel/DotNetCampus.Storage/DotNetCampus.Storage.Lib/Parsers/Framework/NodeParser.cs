using DotNetCampus.Storage.StorageNodes;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetCampus.Storage.Parsers.Contexts;

namespace DotNetCampus.Storage.Parsers;

/// <summary>
/// 存储转换器
/// SaveInfo （或 SaveInfo 里面的子属性） -> StorageNode
/// </summary>
public abstract class NodeParser
{
    /// <summary>
    /// 用于转换的目标类型
    /// </summary>
    /// SaveInfo （或 SaveInfo 里面的子属性）
    public abstract Type TargetType { get; }

    /// <summary>
    /// 写在存储中的名称
    /// </summary>
    /// 用于扩展内容，以及泛型
    public abstract string? TargetStorageName { get; }

    /// <summary>
    /// 从存储节点中解析出对象
    /// </summary>
    /// <param name="node"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public abstract object Parse(StorageNode node, in ParseNodeContext context);

    /// <summary>
    /// 从对象中反解析出存储节点
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public abstract StorageNode Deparse(object obj, in DeparseNodeContext context);
}

public abstract class NodeParser<T> : NodeParser
{
    public override Type TargetType => typeof(T);

    public override object Parse(StorageNode node, in ParseNodeContext context)
    {
        return ParseCore(node, in context)!;
    }

    protected internal abstract T ParseCore(StorageNode node, in ParseNodeContext context);

    public override StorageNode Deparse(object obj, in DeparseNodeContext context)
    {
        if (obj is not T t)
        {
            throw new ArgumentException($"对象类型错误，期望类型：{typeof(T).FullName}，实际类型：{obj.GetType().FullName}");
        }
        return DeparseCore(t, in context);
    }

    protected internal abstract StorageNode DeparseCore(T obj, in DeparseNodeContext context);
}