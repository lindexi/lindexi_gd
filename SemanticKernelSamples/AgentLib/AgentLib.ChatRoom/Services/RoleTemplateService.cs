using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
#if !NET6_0
using System.Text.Json.Serialization;
#endif
using System.Threading;
using System.Threading.Tasks;

using AgentLib.ChatRoom.Model;

namespace AgentLib.ChatRoom.Services;

/// <summary>
/// 角色模板管理服务。负责模板的加载、保存、删除、转换以及预置模板初始化。
/// 模板独立持久化在指定目录下，每个模板一个 JSON 文件，与会话持久化解耦。
/// </summary>
public sealed class RoleTemplateService
{
    private readonly string _templatesFolder;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    /// <summary>
    /// 使用指定的模板持久化目录创建服务。
    /// </summary>
    /// <param name="templatesFolder">模板 JSON 文件的存储目录。不存在时自动创建。</param>
    public RoleTemplateService(string templatesFolder)
    {
        if (string.IsNullOrWhiteSpace(templatesFolder))
        {
            throw new ArgumentException("模板目录路径不能为空。", nameof(templatesFolder));
        }
        _templatesFolder = templatesFolder;
        Directory.CreateDirectory(_templatesFolder);
    }

    /// <summary>
    /// 加载所有模板。损坏的模板文件会被跳过。
    /// </summary>
    /// <returns>所有有效模板的列表，按更新时间降序排列。</returns>
    public IReadOnlyList<RoleTemplate> LoadAll()
    {
        var result = new List<RoleTemplate>();

        foreach (string file in Directory.EnumerateFiles(_templatesFolder, "*.json"))
        {
            try
            {
                string json = File.ReadAllText(file);
#if NET6_0
                RoleTemplate? template = JsonSerializer.Deserialize<RoleTemplate>(json);
#else
                RoleTemplate? template = JsonSerializer.Deserialize(json, RoleTemplateJsonSerializerContext.Default.RoleTemplate);
#endif
                if (template is not null && !string.IsNullOrEmpty(template.TemplateId))
                {
                    result.Add(template);
                }
            }
            catch (JsonException)
            {
                // 损坏的 JSON 文件跳过，不影响其他模板加载
            }
            catch (IOException)
            {
                // 读取失败的文件跳过
            }
        }

        return result
            .OrderByDescending(t => t.UpdatedAt)
            .ToList();
    }

    /// <summary>
    /// 保存或更新模板。以 <see cref="RoleTemplate.TemplateId"/> 作为文件名。
    /// </summary>
    /// <param name="template">要保存的模板。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task SaveAsync(RoleTemplate template, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(template);
        if (string.IsNullOrWhiteSpace(template.TemplateId))
        {
            throw new ArgumentException("模板 ID 不能为空。", nameof(template.TemplateId));
        }

        template.UpdatedAt = DateTimeOffset.Now;

        string filePath = GetTemplateFilePath(template.TemplateId);

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
#if NET6_0
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
            };
            string json = JsonSerializer.Serialize(template, options);
#else
            string json = JsonSerializer.Serialize(template, RoleTemplateJsonSerializerContext.Default.RoleTemplate);
#endif
            await File.WriteAllTextAsync(filePath, json, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>
    /// 删除指定模板。
    /// </summary>
    /// <param name="templateId">模板 ID。</param>
    public void Delete(string templateId)
    {
        if (string.IsNullOrWhiteSpace(templateId))
        {
            throw new ArgumentException("模板 ID 不能为空。", nameof(templateId));
        }
        string filePath = GetTemplateFilePath(templateId);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    /// <summary>
    /// 从模板创建角色定义。生成新的 <see cref="ChatRoomRoleDefinition.RoleId"/>，
    /// 其余属性从 <see cref="RoleTemplate.Definition"/> 复制。
    /// </summary>
    /// <param name="template">角色模板。</param>
    /// <returns>可用于添加到会话的角色定义。</returns>
    public ChatRoomRoleDefinition ToDefinition(RoleTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);

        var source = template.Definition;
        return new ChatRoomRoleDefinition
        {
            RoleId = Guid.NewGuid().ToString("N"),
            RoleName = source.RoleName,
            SystemPrompt = source.SystemPrompt,
            IsHuman = source.IsHuman,
            ModelProviderId = source.ModelProviderId,
            ModelId = source.ModelId,
            SkillFolders = [..source.SkillFolders],
            Tools = [..source.Tools],
            MemoryContent = source.MemoryContent,
            ParticipationMode = source.ParticipationMode,
            IsManagerRole = source.IsManagerRole,
        };
    }

    /// <summary>
    /// 从会话角色定义创建模板。生成新的 <see cref="RoleTemplate.TemplateId"/>，
    /// 复制角色定义属性。
    /// </summary>
    /// <param name="definition">会话中的角色定义。</param>
    /// <param name="name">模板显示名。</param>
    /// <param name="description">模板描述。</param>
    /// <param name="category">模板分类。</param>
    /// <param name="tags">标签列表。</param>
    /// <returns>新的角色模板实例。</returns>
    public RoleTemplate FromDefinition(
        ChatRoomRoleDefinition definition,
        string name,
        string description,
        string category = "通用",
        List<string>? tags = null)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var now = DateTimeOffset.Now;
        return new RoleTemplate
        {
            TemplateId = Guid.NewGuid().ToString("N"),
            Name = name,
            Description = description,
            Category = string.IsNullOrWhiteSpace(category) ? "通用" : category,
            Tags = tags ?? [],
            CreatedAt = now,
            UpdatedAt = now,
            IsPreset = false,
            Definition = new ChatRoomRoleDefinition
            {
                RoleId = definition.RoleId,
                RoleName = definition.RoleName,
                SystemPrompt = definition.SystemPrompt,
                IsHuman = definition.IsHuman,
                ModelProviderId = definition.ModelProviderId,
                ModelId = definition.ModelId,
                SkillFolders = [..definition.SkillFolders],
                Tools = [..definition.Tools],
                MemoryContent = definition.MemoryContent,
                ParticipationMode = definition.ParticipationMode,
                IsManagerRole = definition.IsManagerRole,
            },
        };
    }

    /// <summary>
    /// 使用会话角色定义更新已有模板。
    /// </summary>
    /// <param name="template">要更新的模板。</param>
    /// <param name="definition">会话中的角色定义。</param>
    /// <param name="name">模板显示名。</param>
    /// <param name="description">模板描述。</param>
    /// <param name="category">模板分类。</param>
    /// <param name="tags">标签列表。</param>
    public void UpdateFromDefinition(
        RoleTemplate template,
        ChatRoomRoleDefinition definition,
        string name,
        string description,
        string category = "通用",
        List<string>? tags = null)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(definition);

        template.Name = name;
        template.Description = description;
        template.Category = string.IsNullOrWhiteSpace(category) ? "通用" : category;
        template.Tags.Clear();
        template.Tags.AddRange(tags ?? []);
        template.Definition = new ChatRoomRoleDefinition
        {
            RoleId = definition.RoleId,
            RoleName = definition.RoleName,
            SystemPrompt = definition.SystemPrompt,
            IsHuman = definition.IsHuman,
            ModelProviderId = definition.ModelProviderId,
            ModelId = definition.ModelId,
            SkillFolders = [..definition.SkillFolders],
            Tools = [..definition.Tools],
            MemoryContent = definition.MemoryContent,
            ParticipationMode = definition.ParticipationMode,
            IsManagerRole = definition.IsManagerRole,
        };
    }

    /// <summary>
    /// 首次启动时写入预置模板。仅当模板目录完全为空时执行。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task EnsurePresetTemplatesAsync(CancellationToken cancellationToken = default)
    {
        if (Directory.EnumerateFiles(_templatesFolder, "*.json").Any())
        {
            return;
        }

        foreach (RoleTemplate preset in PresetTemplates.GetPresets())
        {
            await SaveAsync(preset, cancellationToken).ConfigureAwait(false);
        }
    }

    private string GetTemplateFilePath(string templateId) => Path.Join(_templatesFolder, $"{templateId}.json");
}
