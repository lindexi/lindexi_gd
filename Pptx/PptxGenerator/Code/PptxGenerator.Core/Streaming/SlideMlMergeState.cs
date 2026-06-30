using System.Xml.Linq;

namespace PptxGenerator.Streaming;

/// <summary>
/// 合并器运行时状态，包含 DOM 树、Id 索引、StyleId 索引和悬空元素列表。
/// 提供 <see cref="Clone"/> 方法用于版本回滚前的深拷贝快照。
/// </summary>
internal sealed class SlideMlMergeState
{
    /// <summary>
    /// 初始化 <see cref="SlideMlMergeState"/> 的新实例，所有状态为空。
    /// </summary>
    public SlideMlMergeState()
    {
        IdIndex = new Dictionary<string, XElement>(StringComparer.Ordinal);
        StyleIdIndex = new Dictionary<string, XElement>(StringComparer.Ordinal);
        DanglingElements = new List<XElement>();
    }

    /// <summary>
    /// 文档树，可能为 <see langword="null"/>（尚未接受 Page 片段）。
    /// </summary>
    public XDocument? Document { get; set; }

    /// <summary>
    /// Id 到元素的索引，用于按 Id 查找元素。
    /// </summary>
    public Dictionary<string, XElement> IdIndex { get; }

    /// <summary>
    /// StyleId 到元素的索引，用于 StyleFrom 引用。
    /// </summary>
    public Dictionary<string, XElement> StyleIdIndex { get; }

    /// <summary>
    /// 悬空元素列表（不在 Page 子树内的模板元素），保留 StyleId 引用能力。
    /// </summary>
    public List<XElement> DanglingElements { get; }

    /// <summary>
    /// 创建当前状态的深拷贝。
    /// DOM 树和悬空元素均独立拷贝，索引基于拷贝后的节点重建。
    /// </summary>
    /// <returns>与当前状态完全独立的副本。</returns>
    public SlideMlMergeState Clone()
    {
        var clone = new SlideMlMergeState();

        // 深拷贝文档树
        clone.Document = Document is not null ? new XDocument(Document) : null;

        // 深拷贝悬空元素列表
        foreach (var element in DanglingElements)
        {
            clone.DanglingElements.Add(new XElement(element));
        }

        // 基于拷贝后的树和悬空元素重建索引
        if (clone.Document?.Root is not null)
        {
            RegisterElementAndDescendants(clone.Document.Root, clone.IdIndex, clone.StyleIdIndex);
        }

        foreach (var dangling in clone.DanglingElements)
        {
            RegisterElementAndDescendants(dangling, clone.IdIndex, clone.StyleIdIndex);
        }

        return clone;
    }

    /// <summary>
    /// 清空所有状态。
    /// </summary>
    public void Clear()
    {
        Document = null;
        IdIndex.Clear();
        StyleIdIndex.Clear();
        DanglingElements.Clear();
    }

    /// <summary>
    /// 输出当前 DOM 树的 XML 字符串。
    /// </summary>
    /// <returns>XML 字符串；如果尚无内容，返回空字符串。</returns>
    public string GetXml()
    {
        return Document?.ToString() ?? string.Empty;
    }

    /// <summary>
    /// 注册元素及其所有子元素到 Id 和 StyleId 索引。
    /// </summary>
    private static void RegisterElementAndDescendants(
        XElement root,
        Dictionary<string, XElement> idIndex,
        Dictionary<string, XElement> styleIdIndex)
    {
        foreach (var element in root.DescendantsAndSelf())
        {
            var id = (string?)element.Attribute("Id");
            if (id is not null)
            {
                idIndex[id] = element;
            }

            var styleId = (string?)element.Attribute("StyleId");
            if (styleId is not null)
            {
                styleIdIndex[styleId] = element;
            }
        }
    }
}
