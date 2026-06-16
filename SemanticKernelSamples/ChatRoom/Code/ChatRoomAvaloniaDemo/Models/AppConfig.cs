using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ChatRoomAvaloniaDemo.Models;

/// <summary>
/// 全局应用配置。包含持久化路径、模型提供商列表等全局设置。
/// 支持 JSON 序列化/反序列化以持久化到本地文件。
/// </summary>
public sealed class AppConfig
{
    /// <summary>
    /// 默认配置文件名。
    /// </summary>
    public const string DefaultFileName = "AppConfiguration.json";

    /// <summary>
    /// 持久化根目录路径。聊天室会话数据将存储在此目录下。
    /// </summary>
    public string PersistenceBasePath { get; set; } = string.Empty;

    /// <summary>
    /// 技能文件夹基础路径。
    /// </summary>
    public string SkillFoldersBasePath { get; set; } = string.Empty;

    /// <summary>
    /// 默认最大对话轮次。
    /// </summary>
    public int DefaultMaxRounds { get; set; } = 10;

    /// <summary>
    /// 全局默认模型所属提供商 ID。
    /// </summary>
    public string DefaultModelProviderId { get; set; } = string.Empty;

    /// <summary>
    /// 全局默认模型名称。
    /// </summary>
    public string DefaultModelId { get; set; } = string.Empty;

    /// <summary>
    /// 配置文件路径（不序列化，仅用于运行时跟踪配置文件位置）。
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string ConfigFilePath { get; set; } = string.Empty;

    /// <summary>
    /// 模型提供商配置列表。
    /// </summary>
    public ObservableCollection<ModelProviderConfig> Providers { get; init; } = [];

    /// <summary>
    /// 将配置保存到指定路径的 JSON 文件。
    /// </summary>
    /// <param name="filePath">文件路径。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task SaveAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(this, options);
        await File.WriteAllTextAsync(filePath, json, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 从指定路径的 JSON 文件加载配置。
    /// </summary>
    /// <param name="filePath">文件路径。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>加载的配置；如果文件不存在则返回 <see langword="null"/>。</returns>
    public static async Task<AppConfig?> LoadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        string json = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Deserialize<AppConfig>(json);
    }
}
