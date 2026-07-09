using System.Xml.Linq;
using PptxGenerator.Models;

namespace PptxGenerator.Streaming;

/// <summary>
/// 维护 XDocument DOM 树与 Id 索引，逐片段合并 SlideML XML。
/// 接收完整的 XML 片段，根据元素类型（Page、Panel/Rect/TextElement/Image、Remove）和 Id 执行合并操作。
/// 采用双缓冲机制：每次合并从已确认状态克隆出工作副本，在副本上执行合并；
/// 成功则晋升工作副本为已确认状态，出错则丢弃工作副本，防止错误片段持续污染 DOM 树。
/// </summary>
public sealed class SlideMlStreamingMerger
{
    /// <summary>
    /// 已确认正确的合并状态，不被合并操作直接修改。
    /// </summary>
    private SlideMlMergeState _committed = new();

    /// <summary>
    /// 当前工作副本，合并操作在此上执行。
    /// 为 <see langword="null"/> 表示无待提交的工作（上次合并已成功晋升）。
    /// </summary>
    private SlideMlMergeState? _working;

    /// <summary>
    /// 接收一个完整 XML 片段并合并到 DOM 树。
    /// 从已确认状态克隆出工作副本，在副本上执行合并；成功则晋升为已确认状态，出错则保留工作副本待回滚。
    /// </summary>
    /// <param name="fragmentXml">完整的 XML 片段字符串。</param>
    /// <param name="context">渲染上下文，用于收集警告和错误。</param>
    public void AcceptFragment(string fragmentXml, SlideMlPipelineContext context)
    {
        ArgumentNullException.ThrowIfNull(fragmentXml);
        ArgumentNullException.ThrowIfNull(context);

        // 从已确认状态克隆出工作副本
        _working = _committed.Clone();
        var errorCountBefore = context.Errors.Count;

        // 解析 XML 片段
        XElement fragmentRoot;
        try
        {
            fragmentRoot = XElement.Parse(fragmentXml);
        }
        catch (Exception ex)
        {
            context.AddError($"[Error] XML 格式错误: {ex.Message}");
            // 出错：保留 _working（含错误状态），等外部调 RollbackLastVersion 丢弃
            return;
        }

        // 检测同片段内重复 Id
        // 同类型元素的重复 Id 视为警告（允许覆盖替换）；不同类型元素的重复 Id 视为错误（无法替换）
        var fragmentIds = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var element in fragmentRoot.DescendantsAndSelf())
        {
            var id = GetElementId(element);
            if (id is null)
            {
                continue;
            }

            var typeName = element.Name.LocalName;
            if (fragmentIds.TryGetValue(id, out var existingTypeName))
            {
                if (!string.Equals(existingTypeName, typeName, StringComparison.OrdinalIgnoreCase))
                {
                    context.AddError($"[Error] 同一片段内 Id '{id}' 被不同类型元素占用: {existingTypeName} vs {typeName}");
                    // 出错：保留 _working，等外部调 RollbackLastVersion 丢弃
                    return;
                }

                context.AddWarning($"[Warning] 同一片段内 Id '{id}' 出现多次（类型 {typeName}），后续覆盖前者");
            }
            else
            {
                fragmentIds[id] = typeName;
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
        else if (SlideMlParser.IsIdRequiredElement(localName))
        {
            ProcessElement(fragmentRoot, context);
        }
        else
        {
            context.AddError($"[Error] 未知的片段根元素: {localName}");
        }

        // 合并成功：晋升工作副本为已确认状态
        if (context.Errors.Count == errorCountBefore)
        {
            _committed = _working;
            _working = null;
        }
        // 出错：保留 _working（含错误状态），等外部调 RollbackLastVersion 丢弃
    }

    /// <summary>
    /// 输出合并完成的完整 XML 字符串。
    /// 优先返回工作副本（出错时的当前状态），否则返回已确认状态。
    /// </summary>
    /// <returns>当前合并状态的 XML。如果尚无内容，返回空字符串。</returns>
    public string GetMergedXml()
    {
        return (_working ?? _committed).GetXml();
    }

    /// <summary>
    /// 丢弃出错的工作副本，恢复到已确认状态。
    /// 如果没有出错的工作副本则不做任何操作。
    /// </summary>
    /// <returns>是否成功回滚（存在出错的工作副本时返回 true）。</returns>
    public bool RollbackLastVersion()
    {
        if (_working is null)
        {
            return false;
        }

        _working = null;
        return true;
    }

    /// <summary>
    /// 清空状态以复用，重置已确认状态并丢弃工作副本。
    /// </summary>
    public void Reset()
    {
        _committed = new SlideMlMergeState();
        _working = null;
    }

    /// <summary>
    /// 处理 Page 片段：首次创建文档，后续合并属性与子元素。
    /// 如果当前文档根元素不是 Page（悬空元素作为根），将其降级为悬空元素。
    /// </summary>
    /// <param name="fragmentRoot">片段根元素。</param>
    /// <param name="context">渲染上下文。</param>
    private void ProcessPage(XElement fragmentRoot, SlideMlPipelineContext context)
    {
        if (_working!.Document is null)
        {
            // 首次接受 Page：创建文档
            _working.Document = new XDocument(fragmentRoot);
            RegisterIdElements(fragmentRoot);
            RegisterStyleIdElements(fragmentRoot, context);
        }
        else
        {
            var currentRoot = _working.Document.Root!;

            if (!string.Equals(currentRoot.Name.LocalName, "Page", StringComparison.OrdinalIgnoreCase))
            {
                // 当前根元素不是 Page（是之前作为根的悬空元素），降级为悬空元素
                // 先从文档中分离（不从 _idIndex 移除，悬空元素仍可供 StyleFrom 引用）
                currentRoot.Remove();
                _working.DanglingElements.Add(currentRoot);
                // Page 成为新的根元素
                _working.Document = new XDocument(fragmentRoot);
                RegisterIdElements(fragmentRoot);
                RegisterStyleIdElements(fragmentRoot, context);

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
    /// 当目标元素是文档根元素（Page）时，清空整个文档状态。
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

        if (_working!.IdIndex.TryGetValue(targetId, out var targetElement))
        {
            // 目标是文档根元素（Page）时，清空整个文档状态
            if (ReferenceEquals(targetElement, _working.Document?.Root))
            {
                UnregisterIdElements(targetElement);
                UnregisterStyleIdElements(targetElement);
                _working.Document = null;
                _working.DanglingElements.Clear();
                return;
            }

            // 从 _idIndex 和 _styleIdIndex 中移除该元素及其所有子元素
            UnregisterIdElements(targetElement);
            UnregisterStyleIdElements(targetElement);
            // 从悬空元素列表中移除
            RemoveFromDanglingElements(targetElement);
            // 从父节点移除
            if (targetElement.Parent != null)
            {
                targetElement.Remove();
            }
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

        if (_working!.IdIndex.TryGetValue(id, out var existingElement))
        {
            // 类型冲突检查：不同类型的元素共用同一 Id 无法替换合并
            if (!string.Equals(existingElement.Name.LocalName, fragmentRoot.Name.LocalName, StringComparison.OrdinalIgnoreCase))
            {
                context.AddError($"[Error] Id '{id}' 类型冲突: 已有 {existingElement.Name.LocalName}，传入 {fragmentRoot.Name.LocalName}");
                return;
            }

            // 已存在：属性 Merge + 子元素 Merge
            MergeAttributes(fragmentRoot, existingElement);

            if (fragmentRoot.HasElements)
            {
                MergeChildren(existingElement, fragmentRoot, context);
            }
        }
        else
        {
            // 新增元素 — 作为悬空元素（不在 Page 子树内），必须带 StyleId
            if (string.IsNullOrWhiteSpace((string?)fragmentRoot.Attribute("StyleId")))
            {
                context.AddError($"[Error] 悬空元素缺少 StyleId: {id}");
                return;
            }

            if (_working!.Document is null)
            {
                RemoveClearAttributeMarkers(fragmentRoot);
                _working.Document = new XDocument(fragmentRoot);
            }
            else
            {
                RemoveClearAttributeMarkers(fragmentRoot);
                _working.DanglingElements.Add(fragmentRoot);
            }

            RegisterIdElements(fragmentRoot);
            RegisterStyleIdElements(fragmentRoot, context);
        }
    }

    /// <summary>
    /// 合并片段元素属性到目标元素（片段属性覆盖已有属性，空字符串可清除可选属性）。
    /// </summary>
    /// <param name="source">片段元素（属性来源）。</param>
    /// <param name="target">目标元素（接收属性）。</param>
    private static void MergeAttributes(XElement source, XElement target)
    {
        foreach (var attr in source.Attributes())
        {
            if (attr.Value.Length == 0 && CanClearAttribute(attr.Name.LocalName))
            {
                target.SetAttributeValue(attr.Name, null);
                continue;
            }

            target.SetAttributeValue(attr.Name, attr.Value);
        }
    }

    /// <summary>
    /// 判断空字符串是否可作为清除指定属性的语义。
    /// </summary>
    /// <param name="localName">属性本地名称。</param>
    /// <returns>可清除返回 true，否则返回 false。</returns>
    private static bool CanClearAttribute(string localName)
    {
        return !IsRequiredOrContentAttribute(localName);
    }

    /// <summary>
    /// 移除元素上用于清除可选属性的空字符串标记。
    /// </summary>
    /// <param name="element">要处理的元素。</param>
    private static void RemoveClearAttributeMarkers(XElement element)
    {
        foreach (var attr in element.Attributes().ToList())
        {
            if (attr.Value.Length == 0 && CanClearAttribute(attr.Name.LocalName))
            {
                attr.Remove();
            }
        }
    }

    /// <summary>
    /// 判断属性是否为必填属性、索引属性或内容属性。
    /// </summary>
    /// <param name="localName">属性本地名称。</param>
    /// <returns>不可用空字符串清除返回 true，否则返回 false。</returns>
    private static bool IsRequiredOrContentAttribute(string localName)
    {
        return localName is "Id"
            or "StyleId"
            or "TargetId"
            or "Source"
            or "Text"
            or "Offset"
            or "Color";
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

        MergeStructuredChildren(targetParent, fragmentChildren, context);

        // 检查缺少 Id 的子元素并报错
        // 同时处理带 Id 子元素的 StyleFrom
        var fragmentChildrenWithId = new List<XElement>();
        foreach (var child in fragmentChildren)
        {
            if (!SlideMlParser.IsIdRequiredElement(child.Name.LocalName))
            {
                continue;
            }

            var childId = GetElementId(child);
            if (string.IsNullOrWhiteSpace(childId))
            {
                context.AddError($"[Error] 元素缺少 Id: {child.Name.LocalName}");
                continue;
            }

            // 处理 StyleFrom
            ApplyStyleFrom(child, context);
            RemoveClearAttributeMarkers(child);
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
                UnregisterStyleIdElements(existingChildren[i]);
                RemoveFromDanglingElements(existingChildren[i]);
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

        // 注册新插入元素的 Id 和 StyleId 到索引
        foreach (var node in nodesToInsert)
        {
            RegisterIdElements(node);
            RegisterStyleIdElements(node, context);
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
            if (SlideMlParser.IsIdRequiredElement(child.Name.LocalName))
            {
                var childId = GetElementId(child);
                if (string.IsNullOrWhiteSpace(childId))
                {
                    context.AddError($"[Error] 元素缺少 Id: {child.Name.LocalName}");
                    continue;
                }

                ApplyStyleFrom(child, context);
                RemoveClearAttributeMarkers(child);
            }

            if (child.HasElements)
            {
                ProcessNewElementChildren(child, context);
            }
        }
    }

    /// <summary>
    /// 合并不要求 Id 的结构化子元素。
    /// 可重复元素按标签整组替换，单实例元素按最后一个片段值覆盖。
    /// </summary>
    /// <param name="targetParent">目标父元素。</param>
    /// <param name="fragmentChildren">片段中的直接子元素。</param>
    /// <param name="context">渲染上下文。</param>
    private void MergeStructuredChildren(XElement targetParent, IReadOnlyList<XElement> fragmentChildren, SlideMlPipelineContext context)
    {
        var processedRepeatableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var child in fragmentChildren)
        {
            var localName = child.Name.LocalName;
            if (!SlideMlParser.IsStructuredElement(localName))
            {
                continue;
            }

            if (SlideMlParser.IsRepeatableStructuredElement(localName))
            {
                if (!processedRepeatableNames.Add(localName))
                {
                    continue;
                }

                RemoveChildrenByName(targetParent, localName);

                foreach (var structuredChild in fragmentChildren.Where(t => string.Equals(t.Name.LocalName, localName, StringComparison.Ordinal)))
                {
                    var clone = new XElement(structuredChild);
                    targetParent.Add(clone);
                    RegisterIdElements(clone);
                    RegisterStyleIdElements(clone, context);
                }

                continue;
            }

            RemoveChildrenByName(targetParent, localName);
            var singleClone = new XElement(child);
            targetParent.Add(singleClone);
            RegisterIdElements(singleClone);
            RegisterStyleIdElements(singleClone, context);
        }
    }

    /// <summary>
    /// 移除父元素下指定标签名的所有直接子元素，并同步索引。
    /// </summary>
    /// <param name="parent">父元素。</param>
    /// <param name="localName">要移除的标签名。</param>
    private void RemoveChildrenByName(XElement parent, string localName)
    {
        var childrenToRemove = parent.Elements(localName).ToList();
        foreach (var existingChild in childrenToRemove)
        {
            UnregisterIdElements(existingChild);
            UnregisterStyleIdElements(existingChild);
            RemoveFromDanglingElements(existingChild);
            existingChild.Remove();
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

        var sourceStyleId = styleFromAttr.Value;
        // 移除 StyleFrom 属性
        styleFromAttr.Remove();

        if (_working!.StyleIdIndex.TryGetValue(sourceStyleId, out var sourceElement))
        {
            // 复制源元素的全部属性到当前元素（作为默认值，不覆盖已存在的属性）
            foreach (var attr in sourceElement.Attributes())
            {
                // 不复制 Id 和 StyleId 属性
                if (attr.Name.LocalName == "Id" || attr.Name.LocalName == "StyleId")
                {
                    continue;
                }

                if (element.Attribute(attr.Name) is null)
                {
                    element.SetAttributeValue(attr.Name, attr.Value);
                }
            }
        }
        else
        {
            context.AddError($"[Error] StyleFrom 源元素 StyleId 不存在: {sourceStyleId}");
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
            _working!.IdIndex[id] = element;
        }

        foreach (var descendant in element.Descendants())
        {
            var descendantId = GetElementId(descendant);
            if (descendantId is not null)
            {
                _working.IdIndex[descendantId] = descendant;
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
            _working!.IdIndex.Remove(id);
        }

        foreach (var descendant in element.Descendants())
        {
            var descendantId = GetElementId(descendant);
            if (descendantId is not null)
            {
                _working.IdIndex.Remove(descendantId);
            }
        }
    }

    /// <summary>
    /// 注册元素及其所有带 StyleId 的子元素到 _styleIdIndex。
    /// </summary>
    /// <param name="element">要注册的元素。</param>
    /// <param name="context">渲染上下文，用于收集错误。</param>
    private void RegisterStyleIdElements(XElement element, SlideMlPipelineContext context)
    {
        var styleId = (string?)element.Attribute("StyleId");
        if (styleId is not null)
        {
            if (_working!.StyleIdIndex.ContainsKey(styleId))
            {
                context.AddError($"[Error] StyleId 重复: {styleId}");
            }
            else
            {
                _working.StyleIdIndex[styleId] = element;
            }
        }

        foreach (var descendant in element.Descendants())
        {
            var descendantStyleId = (string?)descendant.Attribute("StyleId");
            if (descendantStyleId is not null)
            {
                if (_working.StyleIdIndex.ContainsKey(descendantStyleId))
                {
                    context.AddError($"[Error] StyleId 重复: {descendantStyleId}");
                }
                else
                {
                    _working.StyleIdIndex[descendantStyleId] = descendant;
                }
            }
        }
    }

    /// <summary>
    /// 从 _styleIdIndex 中移除元素及其所有带 StyleId 的子元素。
    /// </summary>
    /// <param name="element">要移除的元素。</param>
    private void UnregisterStyleIdElements(XElement element)
    {
        var styleId = (string?)element.Attribute("StyleId");
        if (styleId is not null)
        {
            _working!.StyleIdIndex.Remove(styleId);
        }

        foreach (var descendant in element.Descendants())
        {
            var descendantStyleId = (string?)descendant.Attribute("StyleId");
            if (descendantStyleId is not null)
            {
                _working.StyleIdIndex.Remove(descendantStyleId);
            }
        }
    }

    /// <summary>
    /// 从 <see cref="SlideMlMergeState.DanglingElements"/> 中移除指定元素（按引用比较）。
    /// </summary>
    /// <param name="element">要移除的元素。</param>
    private void RemoveFromDanglingElements(XElement element)
    {
        for (var i = _working!.DanglingElements.Count - 1; i >= 0; i--)
        {
            if (ReferenceEquals(_working.DanglingElements[i], element))
            {
                _working.DanglingElements.RemoveAt(i);
                return;
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
