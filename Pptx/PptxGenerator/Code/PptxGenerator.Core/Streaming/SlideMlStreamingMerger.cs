using System.Xml.Linq;
using PptxGenerator.Models;

namespace PptxGenerator.Streaming;

/// <summary>
/// 维护 XDocument DOM 树与 Id 索引，逐片段合并 SlideML XML。
/// 接收完整的 XML 片段，根据元素类型（Page、Panel/Rect/TextElement/Image、Remove）和 Id 执行合并操作。
/// </summary>
public sealed class SlideMlStreamingMerger
{
    /// <summary>
    /// 需要校验 Id 的元素类型集合（不含 Page 和 Remove）。
    /// </summary>
    private static readonly HashSet<string> s_idRequiredElements = new(StringComparer.OrdinalIgnoreCase)
    {
        "Panel", "Rect", "TextElement", "Image", "Span", "Fill", "Stroke", "Shadow", "LinearGradient", "Stop"
    };

    private XDocument? _document;
    private readonly Dictionary<string, XElement> _idIndex = new();
    private readonly List<XElement> _danglingElements = new();

    /// <summary>
    /// 接收一个完整 XML 片段并合并到 DOM 树。
    /// </summary>
    /// <param name="fragmentXml">完整的 XML 片段字符串。</param>
    /// <param name="context">渲染上下文，用于收集警告和错误。</param>
    public void AcceptFragment(string fragmentXml, SlideMlPipelineContext context)
    {
        ArgumentNullException.ThrowIfNull(fragmentXml);
        ArgumentNullException.ThrowIfNull(context);

        // 解析 XML 片段
        XElement fragmentRoot;
        try
        {
            fragmentRoot = XElement.Parse(fragmentXml, LoadOptions.PreserveWhitespace);
        }
        catch (Exception ex)
        {
            context.AddError($"[Error] XML 格式错误: {ex.Message}");
            return;
        }

        // 检测同片段内重复 Id
        var fragmentIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var element in fragmentRoot.DescendantsAndSelf())
        {
            var id = GetElementId(element);
            if (id is null)
            {
                continue;
            }

            if (!fragmentIds.Add(id))
            {
                context.AddError($"[Error] 同一片段内出现重复 Id: {id}");
                return;
            }
        }

        var localName = fragmentRoot.Name.LocalName;
        if (string.Equals(localName, "Page", StringComparison.OrdinalIgnoreCase))
        {
            ProcessPage(fragmentRoot, context);
        }
        else if (string.Equals(localName, "Remove", StringComparison.OrdinalIgnoreCase))
        {
            ProcessRemove(fragmentRoot, context);
        }
        else if (s_idRequiredElements.Contains(localName))
        {
            ProcessElement(fragmentRoot, context);
        }
        else
        {
            context.AddError($"[Error] 未知的片段根元素: {localName}");
        }
    }

    /// <summary>
    /// 输出合并完成的完整 XML 字符串。
    /// </summary>
    /// <returns>当前合并状态的 XML。如果尚无内容，返回空字符串。</returns>
    public string GetMergedXml()
    {
        return _document?.ToString() ?? string.Empty;
    }

    /// <summary>
    /// 清空状态以复用。
    /// </summary>
    public void Reset()
    {
        _document = null;
        _idIndex.Clear();
        _danglingElements.Clear();
    }

    /// <summary>
    /// 处理 Page 片段：首次创建文档，后续合并属性与子元素。
    /// 如果当前文档根元素不是 Page（悬空元素作为根），将其降级为悬空元素。
    /// </summary>
    /// <param name="fragmentRoot">片段根元素。</param>
    /// <param name="context">渲染上下文。</param>
    private void ProcessPage(XElement fragmentRoot, SlideMlPipelineContext context)
    {
        if (_document is null)
        {
            // 首次接受 Page：创建文档
            _document = new XDocument(fragmentRoot);
            RegisterIdElements(fragmentRoot);
        }
        else
        {
            var currentRoot = _document.Root!;

            if (!string.Equals(currentRoot.Name.LocalName, "Page", StringComparison.OrdinalIgnoreCase))
            {
                // 当前根元素不是 Page（是之前作为根的悬空元素），降级为悬空元素
                // 先从文档中分离（不从 _idIndex 移除，悬空元素仍可供 StyleFrom 引用）
                currentRoot.Remove();
                _danglingElements.Add(currentRoot);
                // Page 成为新的根元素
                _document = new XDocument(fragmentRoot);
                RegisterIdElements(fragmentRoot);

                // 处理 Page 片段子元素的 StyleFrom 和缺 Id 检查
                ProcessNewElementChildren(fragmentRoot, context);
            }
            else
            {
                // 合并 Page 属性
                MergeAttributes(fragmentRoot, currentRoot);

                // 如果片段 Page 有子元素，执行 MergeChildren
                if (fragmentRoot.HasElements)
                {
                    MergeChildren(currentRoot, fragmentRoot, context);
                }
            }
        }
    }

    /// <summary>
    /// 处理 Remove 片段：从 DOM 树中移除目标元素。
    /// </summary>
    /// <param name="fragmentRoot">片段根元素。</param>
    /// <param name="context">渲染上下文。</param>
    private void ProcessRemove(XElement fragmentRoot, SlideMlPipelineContext context)
    {
        var targetId = (string?)fragmentRoot.Attribute("TargetId");
        if (string.IsNullOrWhiteSpace(targetId))
        {
            context.AddError("[Error] Remove 缺少 TargetId 属性");
            return;
        }

        if (_idIndex.TryGetValue(targetId, out var targetElement))
        {
            // 从 _idIndex 中移除该元素及其所有带 Id 的子元素
            UnregisterIdElements(targetElement);
            // 从父节点移除
            targetElement.Remove();
        }
        else
        {
            context.AddWarning($"[Warning] Remove 目标不存在: {targetId}");
        }
    }

    /// <summary>
    /// 处理 Panel/Rect/TextElement/Image 等带 Id 的元素片段。
    /// </summary>
    /// <param name="fragmentRoot">片段根元素。</param>
    /// <param name="context">渲染上下文。</param>
    private void ProcessElement(XElement fragmentRoot, SlideMlPipelineContext context)
    {
        var id = GetElementId(fragmentRoot);
        if (string.IsNullOrWhiteSpace(id))
        {
            context.AddError($"[Error] 元素缺少 Id: {fragmentRoot.Name.LocalName}");
            return;
        }

        // 处理 StyleFrom
        ApplyStyleFrom(fragmentRoot, context);

        if (_idIndex.TryGetValue(id, out var existingElement))
        {
            // 已存在：属性 Merge + 子元素 Merge
            MergeAttributes(fragmentRoot, existingElement);

            if (fragmentRoot.HasElements)
            {
                MergeChildren(existingElement, fragmentRoot, context);
            }
        }
        else
        {
            // 新增元素
            if (_document is null)
            {
                // _document 为 null，创建文档以该元素为根
                _document = new XDocument(fragmentRoot);
            }
            else
            {
                var currentRoot = _document.Root!;
                if (string.Equals(currentRoot.Name.LocalName, "Page", StringComparison.OrdinalIgnoreCase))
                {
                    // Page 已存在，顶层片段元素不在 Page 子树内，作为悬空元素
                    _danglingElements.Add(fragmentRoot);
                }
                else
                {
                    // 当前根不是 Page，新元素也作为悬空元素
                    _danglingElements.Add(fragmentRoot);
                }
            }

            RegisterIdElements(fragmentRoot);
        }
    }

    /// <summary>
    /// 合并片段元素属性到目标元素（片段属性覆盖已有属性）。
    /// </summary>
    /// <param name="source">片段元素（属性来源）。</param>
    /// <param name="target">目标元素（接收属性）。</param>
    private static void MergeAttributes(XElement source, XElement target)
    {
        foreach (var attr in source.Attributes())
        {
            target.SetAttributeValue(attr.Name, attr.Value);
        }
    }

    /// <summary>
    /// 子元素 Merge 算法（§14）：定位锚点、移除冲突、插入片段子元素。
    /// 同时处理缺 Id 子元素报错和 StyleFrom 属性。
    /// </summary>
    /// <param name="targetParent">目标父容器。</param>
    /// <param name="fragmentParent">片段父容器。</param>
    /// <param name="context">渲染上下文。</param>
    private void MergeChildren(XElement targetParent, XElement fragmentParent, SlideMlPipelineContext context)
    {
        var fragmentChildren = fragmentParent.Elements().ToList();
        if (fragmentChildren.Count == 0)
        {
            return;
        }

        // 检查缺少 Id 的子元素并报错
        // 同时处理带 Id 子元素的 StyleFrom
        var fragmentChildrenWithId = new List<XElement>();
        foreach (var child in fragmentChildren)
        {
            var childId = GetElementId(child);
            if (string.IsNullOrWhiteSpace(childId))
            {
                context.AddError($"[Error] 元素缺少 Id: {child.Name.LocalName}");
                continue;
            }

            // 处理 StyleFrom
            ApplyStyleFrom(child, context);
            fragmentChildrenWithId.Add(child);
        }

        if (fragmentChildrenWithId.Count == 0)
        {
            return;
        }

        // 获取当前子元素列表 L
        var existingChildren = targetParent.Elements().ToList();

        // 片段中所有 Id 的集合
        var fragmentIdSet = new HashSet<string>(
            fragmentChildrenWithId.Select(GetElementId)!,
            StringComparer.Ordinal);

        // ① 定位锚点 P
        // 从 f₁ 开始依次检查 F 中每个元素的 Id 是否在 L 中存在
        int anchorPos = -1;
        foreach (var fragmentChild in fragmentChildrenWithId)
        {
            var fragmentChildId = GetElementId(fragmentChild)!;
            for (var i = 0; i < existingChildren.Count; i++)
            {
                if (GetElementId(existingChildren[i]) == fragmentChildId)
                {
                    anchorPos = i;
                    break;
                }
            }

            if (anchorPos >= 0)
            {
                break;
            }
        }

        // 如果 F 中所有元素的 Id 在 L 中都不存在 → P = |L|（末尾位置）
        if (anchorPos < 0)
        {
            anchorPos = existingChildren.Count;
        }

        // ② 移除冲突：遍历 L，如果 Id 在 F 中出现则移除
        // 记录被移除元素在原始 L 中的位置，用于计算插入位置
        var removedIndices = new HashSet<int>();
        for (var i = 0; i < existingChildren.Count; i++)
        {
            var existingChildId = GetElementId(existingChildren[i]);
            if (existingChildId is not null && fragmentIdSet.Contains(existingChildId))
            {
                UnregisterIdElements(existingChildren[i]);
                existingChildren[i].Remove();
                removedIndices.Add(i);
            }
        }

        // ③ 计算插入位置
        // anchorPos 是基于原始 L 的，需要映射到移除冲突后的位置
        // 计算在 anchorPos 之前有多少元素被移除
        int removedBeforeAnchor = 0;
        for (var i = 0; i < anchorPos; i++)
        {
            if (removedIndices.Contains(i))
            {
                removedBeforeAnchor++;
            }
        }

        int insertPos = anchorPos - removedBeforeAnchor;

        // 获取移除后的子元素列表
        var currentChildren = targetParent.Elements().ToList();

        // 若 P > |currentChildren| → 追加到末尾
        if (insertPos > currentChildren.Count)
        {
            insertPos = currentChildren.Count;
        }

        // 将片段子元素深拷贝并插入
        var nodesToInsert = fragmentChildrenWithId.Select(f => new XElement(f)).ToList();

        if (insertPos >= currentChildren.Count)
        {
            // 追加到末尾
            foreach (var node in nodesToInsert)
            {
                targetParent.Add(node);
            }
        }
        else
        {
            // 在指定位置插入
            var refNode = currentChildren[insertPos];
            foreach (var node in nodesToInsert)
            {
                refNode.AddBeforeSelf(node);
            }
        }

        // 注册新插入元素的 Id 到 _idIndex
        foreach (var node in nodesToInsert)
        {
            RegisterIdElements(node);
        }

        // 递归处理：对每个片段子元素，如果它有子元素，递归 Merge
        foreach (var fragmentChild in fragmentChildrenWithId)
        {
            if (!fragmentChild.HasElements)
            {
                continue;
            }

            var fragmentChildId = GetElementId(fragmentChild)!;
            // 找到目标中对应的元素
            var targetChild = targetParent.Elements()
                .FirstOrDefault(e => GetElementId(e) == fragmentChildId);

            if (targetChild is not null)
            {
                MergeChildren(targetChild, fragmentChild, context);
            }
        }
    }

    /// <summary>
    /// 处理新增元素的子元素：检查缺 Id 报错、应用 StyleFrom、递归处理。
    /// 不执行 Merge 算法，仅做初始化处理。
    /// </summary>
    /// <param name="parent">父元素。</param>
    /// <param name="context">渲染上下文。</param>
    private void ProcessNewElementChildren(XElement parent, SlideMlPipelineContext context)
    {
        foreach (var child in parent.Elements())
        {
            var childId = GetElementId(child);
            if (string.IsNullOrWhiteSpace(childId))
            {
                context.AddError($"[Error] 元素缺少 Id: {child.Name.LocalName}");
                continue;
            }

            ApplyStyleFrom(child, context);

            if (child.HasElements)
            {
                ProcessNewElementChildren(child, context);
            }
        }
    }

    /// <summary>
    /// 应用 StyleFrom 属性：从源元素复制属性作为默认值。
    /// </summary>
    /// <param name="element">当前元素。</param>
    /// <param name="context">渲染上下文。</param>
    private void ApplyStyleFrom(XElement element, SlideMlPipelineContext context)
    {
        var styleFromAttr = element.Attribute("StyleFrom");
        if (styleFromAttr is null)
        {
            return;
        }

        var sourceId = styleFromAttr.Value;
        // 移除 StyleFrom 属性
        styleFromAttr.Remove();

        if (_idIndex.TryGetValue(sourceId, out var sourceElement))
        {
            // 复制源元素的全部属性到当前元素（作为默认值，不覆盖已存在的属性）
            foreach (var attr in sourceElement.Attributes())
            {
                if (element.Attribute(attr.Name) is null)
                {
                    element.SetAttributeValue(attr.Name, attr.Value);
                }
            }
        }
        else
        {
            context.AddError($"[Error] StyleFrom 源元素不存在: {sourceId}");
        }
    }

    /// <summary>
    /// 注册元素及其所有带 Id 的子元素到 _idIndex。
    /// </summary>
    /// <param name="element">要注册的元素。</param>
    private void RegisterIdElements(XElement element)
    {
        var id = GetElementId(element);
        if (id is not null)
        {
            _idIndex[id] = element;
        }

        foreach (var descendant in element.Descendants())
        {
            var descendantId = GetElementId(descendant);
            if (descendantId is not null)
            {
                _idIndex[descendantId] = descendant;
            }
        }
    }

    /// <summary>
    /// 从 _idIndex 中移除元素及其所有带 Id 的子元素。
    /// </summary>
    /// <param name="element">要移除的元素。</param>
    private void UnregisterIdElements(XElement element)
    {
        var id = GetElementId(element);
        if (id is not null)
        {
            _idIndex.Remove(id);
        }

        foreach (var descendant in element.Descendants())
        {
            var descendantId = GetElementId(descendant);
            if (descendantId is not null)
            {
                _idIndex.Remove(descendantId);
            }
        }
    }

    /// <summary>
    /// 获取元素的 Id 属性值。
    /// </summary>
    /// <param name="element">XML 元素。</param>
    /// <returns>Id 值；如果没有 Id 属性则返回 null。</returns>
    private static string? GetElementId(XElement element)
    {
        return (string?)element.Attribute("Id");
    }
}
