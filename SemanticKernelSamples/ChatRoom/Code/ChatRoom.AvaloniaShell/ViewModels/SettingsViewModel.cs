using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

using AgentLib.ChatRoom;
using AgentLib.ChatRoom.Configuration;
using AgentLib.ChatRoom.Services;

namespace ChatRoom.AvaloniaShell.ViewModels;

/// <summary>
/// 模型编辑项 ViewModel。
/// </summary>
public sealed class ModelEditViewModel : ViewModelBase
{
    private readonly Action<ModelEditViewModel>? _removeAction;
    private string _modelName = string.Empty;
    private string? _modelId;
    private bool _isFlash;
    private bool _isVision;

    /// <summary>
    /// 删除模型命令。
    /// </summary>
    public ICommand RemoveCommand { get; }

    /// <summary>
    /// 模型显示名称。
    /// </summary>
    public string ModelName
    {
        get => _modelName;
        set => SetField(ref _modelName, value);
    }

    /// <summary>
    /// 实际传给 API 的模型 ID。
    /// </summary>
    public string? ModelId
    {
        get => _modelId;
        set => SetField(ref _modelId, value);
    }

    /// <summary>
    /// 是否为快速模型。
    /// </summary>
    public bool IsFlash
    {
        get => _isFlash;
        set => SetField(ref _isFlash, value);
    }

    /// <summary>
    /// 是否支持视觉输入。
    /// </summary>
    public bool IsVision
    {
        get => _isVision;
        set => SetField(ref _isVision, value);
    }

    /// <summary>
    /// 创建空的模型编辑项。
    /// </summary>
    public ModelEditViewModel(Action<ModelEditViewModel>? removeAction = null)
    {
        _removeAction = removeAction;
        RemoveCommand = new SimpleCommand(Remove);
    }

    /// <summary>
    /// 从 <see cref="ModelSetting"/> 创建模型编辑项。
    /// </summary>
    public ModelEditViewModel(ModelSetting setting, Action<ModelEditViewModel>? removeAction = null)
        : this(removeAction)
    {
        _modelName = setting.ModelName;
        _modelId = setting.ModelId;
        _isFlash = setting.IsFlash;
        _isVision = setting.IsVision;
    }

    /// <summary>
    /// 转换为 <see cref="ModelSetting"/>。
    /// </summary>
    public ModelSetting ToSetting()
    {
        return new ModelSetting
        {
            ModelName = ModelName.Trim(),
            ModelId = string.IsNullOrWhiteSpace(ModelId) ? null : ModelId.Trim(),
            IsFlash = IsFlash,
            IsVision = IsVision,
        };
    }

    private void Remove()
    {
        _removeAction?.Invoke(this);
    }
}

/// <summary>
/// 提供商编辑项 ViewModel。
/// </summary>
public sealed class ProviderEditViewModel : ViewModelBase
{
    private string _name = string.Empty;
    private string _endpoint = string.Empty;
    private string _key = string.Empty;

    /// <summary>
    /// 提供商名称。
    /// </summary>
    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    /// <summary>
    /// API 地址。
    /// </summary>
    public string Endpoint
    {
        get => _endpoint;
        set => SetField(ref _endpoint, value);
    }

    /// <summary>
    /// API 密钥。
    /// </summary>
    public string Key
    {
        get => _key;
        set => SetField(ref _key, value);
    }

    /// <summary>
    /// 模型列表。
    /// </summary>
    public ObservableCollection<ModelEditViewModel> Models { get; } = [];

    /// <summary>
    /// 添加模型命令。
    /// </summary>
    public ICommand AddModelCommand { get; }

    /// <summary>
    /// 删除模型命令。
    /// </summary>
    public ICommand RemoveModelCommand { get; }

    /// <summary>
    /// 从 <see cref="ProviderSetting"/> 创建提供商编辑项。
    /// </summary>
    public ProviderEditViewModel(ProviderSetting setting)
    {
        _name = setting.Name;
        _endpoint = setting.Endpoint;
        _key = setting.Key;

        foreach (ModelSetting model in setting.Models)
        {
            Models.Add(new ModelEditViewModel(model, RemoveModel));
        }

        AddModelCommand = new SimpleCommand(AddModel);
        RemoveModelCommand = new SimpleCommand<ModelEditViewModel>(RemoveModel);
    }

    /// <summary>
    /// 创建空的提供商编辑项。
    /// </summary>
    public ProviderEditViewModel()
    {
        AddModelCommand = new SimpleCommand(AddModel);
        RemoveModelCommand = new SimpleCommand<ModelEditViewModel>(RemoveModel);
    }

    /// <summary>
    /// 转换为 <see cref="ProviderSetting"/>。
    /// </summary>
    public ProviderSetting ToSetting()
    {
        var models = Models
            .Where(m => !string.IsNullOrWhiteSpace(m.ModelName))
            .Select(m => m.ToSetting())
            .ToList();

        return new ProviderSetting
        {
            Name = Name,
            Endpoint = Endpoint,
            Key = Key,
            Models = models,
        };
    }

    private void AddModel()
    {
        Models.Add(new ModelEditViewModel(RemoveModel));
    }

    private void RemoveModel(ModelEditViewModel? model)
    {
        if (model is not null)
        {
            Models.Remove(model);
        }
    }
}

/// <summary>
/// 设置页 ViewModel。
/// </summary>
public sealed class SettingsViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService;
    private readonly ChatRoomService _chatRoomService;
    private AppSettings _appSettings;

    private string _persistencePath = string.Empty;
    private int _defaultMaxRounds = 10;
    private string? _primaryModel;

    /// <summary>
    /// 持久化路径。
    /// </summary>
    public string PersistencePath
    {
        get => _persistencePath;
        set => SetField(ref _persistencePath, value);
    }

    /// <summary>
    /// 默认最大轮次。
    /// </summary>
    public int DefaultMaxRounds
    {
        get => _defaultMaxRounds;
        set => SetField(ref _defaultMaxRounds, value);
    }

    /// <summary>
    /// 全局首选模型。
    /// </summary>
    public string? PrimaryModel
    {
        get => _primaryModel;
        set => SetField(ref _primaryModel, value);
    }

    /// <summary>
    /// 提供商列表。
    /// </summary>
    public ObservableCollection<ProviderEditViewModel> Providers { get; } = [];

    /// <summary>
    /// 保存命令。
    /// </summary>
    public ICommand SaveCommand { get; }

    /// <summary>
    /// 返回命令。
    /// </summary>
    public ICommand BackCommand { get; }

    /// <summary>
    /// 添加提供商命令。
    /// </summary>
    public ICommand AddProviderCommand { get; }

    /// <summary>
    /// 删除提供商命令。
    /// </summary>
    public ICommand RemoveProviderCommand { get; }

    /// <summary>
    /// 返回请求事件。
    /// </summary>
    public event EventHandler? BackRequested;

    /// <summary>
    /// 使用指定的服务创建设置 ViewModel。
    /// </summary>
    /// <param name="settingsService">设置持久化服务。</param>
    /// <param name="chatRoomService">聊天室服务。</param>
    /// <param name="appSettings">已异步加载的应用设置。</param>
    public SettingsViewModel(
        SettingsService settingsService,
        ChatRoomService chatRoomService,
        AppSettings appSettings)
    {
        ArgumentNullException.ThrowIfNull(settingsService);
        ArgumentNullException.ThrowIfNull(chatRoomService);
        ArgumentNullException.ThrowIfNull(appSettings);
        _settingsService = settingsService;
        _chatRoomService = chatRoomService;
        _appSettings = appSettings;

        _persistencePath = _appSettings.PersistencePath;
        _defaultMaxRounds = _appSettings.DefaultMaxRounds;
        _primaryModel = _appSettings.PrimaryModel;

        foreach (ProviderSetting p in _appSettings.Providers)
        {
            Providers.Add(new ProviderEditViewModel(p));
        }

        SaveCommand = new SimpleAsyncCommand(SaveAsync);
        BackCommand = new SimpleCommand(() => BackRequested?.Invoke(this, EventArgs.Empty));
        AddProviderCommand = new SimpleCommand(() => Providers.Add(new ProviderEditViewModel()));
        RemoveProviderCommand = new SimpleCommand<ProviderEditViewModel>(p =>
        {
            if (p is not null)
            {
                Providers.Remove(p);
            }
        });
    }

    private async Task SaveAsync()
    {
        IsBusy = true;
        try
        {
            _appSettings.PersistencePath = _persistencePath;
            _appSettings.DefaultMaxRounds = _defaultMaxRounds;
            _appSettings.PrimaryModel = _primaryModel;

            _appSettings.Providers.Clear();
            foreach (ProviderEditViewModel p in Providers)
            {
                _appSettings.Providers.Add(p.ToSetting());
            }

            await _settingsService.SaveAsync(_appSettings).ConfigureAwait(false);

            // 热更新：更新 ModelProviderService 内部设置并重新注册模型提供商
            _chatRoomService.RefreshProviders(_appSettings);

            BackRequested?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
