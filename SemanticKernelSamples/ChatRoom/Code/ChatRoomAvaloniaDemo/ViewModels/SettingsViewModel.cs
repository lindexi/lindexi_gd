using ChatRoomAvaloniaDemo.Models;

using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace ChatRoomAvaloniaDemo.ViewModels;

/// <summary>
/// 设置页 ViewModel。管理模型提供商列表和全局配置项的编辑。
/// </summary>
public sealed class SettingsViewModel
{
    private readonly AppConfig _appConfig;
    private readonly AppConfig _editableCopy;

    /// <summary>
    /// 创建设置页 ViewModel。基于指定应用配置创建可编辑副本。
    /// </summary>
    public SettingsViewModel(AppConfig? appConfig)
    {
        _appConfig = appConfig ?? new AppConfig();
        _editableCopy = new AppConfig
        {
            PersistenceBasePath = _appConfig.PersistenceBasePath,
            SkillFoldersBasePath = _appConfig.SkillFoldersBasePath,
            DefaultMaxRounds = _appConfig.DefaultMaxRounds,
        };

        foreach (var provider in _appConfig.Providers)
        {
            _editableCopy.Providers.Add(new ModelProviderConfig
            {
                ProviderId = provider.ProviderId,
                ProviderName = provider.ProviderName,
                ApiEndpoint = provider.ApiEndpoint,
                ApiKey = provider.ApiKey,
                DefaultModelId = provider.DefaultModelId,
            });
        }

        AddProviderCommand = new DelegateCommand(AddProvider);
        RemoveProviderCommand = new DelegateCommand<ModelProviderConfig?>(RemoveProvider);
    }

    /// <summary>
    /// 持久化根目录路径。
    /// </summary>
    public string PersistenceBasePath
    {
        get => _editableCopy.PersistenceBasePath;
        set => _editableCopy.PersistenceBasePath = value;
    }

    /// <summary>
    /// 技能文件夹基础路径。
    /// </summary>
    public string SkillFoldersBasePath
    {
        get => _editableCopy.SkillFoldersBasePath;
        set => _editableCopy.SkillFoldersBasePath = value;
    }

    /// <summary>
    /// 默认最大对话轮次。
    /// </summary>
    public int DefaultMaxRounds
    {
        get => _editableCopy.DefaultMaxRounds;
        set => _editableCopy.DefaultMaxRounds = value;
    }

    /// <summary>
    /// 模型提供商列表。
    /// </summary>
    public ObservableCollection<ModelProviderConfig> Providers => _editableCopy.Providers;

    /// <summary>添加提供商命令。</summary>
    public DelegateCommand AddProviderCommand { get; }

    /// <summary>删除提供商命令。</summary>
    public DelegateCommand<ModelProviderConfig?> RemoveProviderCommand { get; }

    /// <summary>
    /// 将可编辑副本的更改写回原始配置对象。
    /// </summary>
    public void ApplyToConfig()
    {
        _appConfig.PersistenceBasePath = _editableCopy.PersistenceBasePath;
        _appConfig.SkillFoldersBasePath = _editableCopy.SkillFoldersBasePath;
        _appConfig.DefaultMaxRounds = _editableCopy.DefaultMaxRounds;

        _appConfig.Providers.Clear();
        foreach (var provider in _editableCopy.Providers)
        {
            _appConfig.Providers.Add(provider);
        }
    }

    private void AddProvider()
    {
        Providers.Add(new ModelProviderConfig
        {
            ProviderId = Guid.NewGuid().ToString("N")[..8],
            ProviderName = "新提供商",
        });
    }

    private void RemoveProvider(ModelProviderConfig? provider)
    {
        if (provider is not null)
        {
            Providers.Remove(provider);
        }
    }
}