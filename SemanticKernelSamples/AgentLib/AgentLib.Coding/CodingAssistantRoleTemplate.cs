namespace AgentLib.Coding;

/// <summary>
/// 描述编程助手的中立模板元数据，不依赖具体宿主的持久化模型。
/// </summary>
public sealed record CodingAssistantRoleTemplate
{
    /// <summary>
    /// 创建编程助手模板描述。
    /// </summary>
    /// <param name="templateId">模板稳定标识。</param>
    /// <param name="name">模板显示名。</param>
    /// <param name="description">模板用途说明。</param>
    /// <param name="category">模板分类。</param>
    /// <param name="tags">模板标签。</param>
    /// <param name="definition">模板中的角色定义。</param>
    public CodingAssistantRoleTemplate(
        string templateId,
        string name,
        string description,
        string category,
        IReadOnlyList<string> tags,
        CodingAssistantRoleDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(templateId))
        {
            throw new ArgumentException("模板标识不能为空。", nameof(templateId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("模板显示名不能为空。", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("模板用途说明不能为空。", nameof(description));
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            throw new ArgumentException("模板分类不能为空。", nameof(category));
        }

        ArgumentNullException.ThrowIfNull(tags);
        ArgumentNullException.ThrowIfNull(definition);
        TemplateId = templateId;
        Name = name;
        Description = description;
        Category = category;
        Tags = tags.ToArray();
        Definition = definition;
    }

    /// <summary>
    /// 获取模板稳定标识。
    /// </summary>
    public string TemplateId { get; }

    /// <summary>
    /// 获取模板显示名。
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 获取模板用途说明。
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// 获取模板分类。
    /// </summary>
    public string Category { get; }

    /// <summary>
    /// 获取模板标签。
    /// </summary>
    public IReadOnlyList<string> Tags { get; }

    /// <summary>
    /// 获取模板中的角色定义。
    /// </summary>
    public CodingAssistantRoleDefinition Definition { get; }
}
