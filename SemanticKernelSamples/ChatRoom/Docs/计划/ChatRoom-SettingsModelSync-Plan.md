# 设置页模型切换即时生效 & 新角色 EndpointManager 初始化

## 背景

当前 `ChatRoomAvaloniaDemo` 存在两个相关缺陷：

1. **设置页切换默认模型后不会立即生效**：在设置页选择新的默认模型并返回聊天界面后，聊天仍然使用旧模型，需要重启应用才能生效。
2. **新创建的角色无法使用任何模型**：通过 UI 新建角色时，`ChatRoomRole` 得到一个未初始化的 `AgentApiEndpointManager`，没有注册任何 Provider，导致该角色无法获取 `IChatClient`。

两个问题的根因相同：`ChatRoomService._endpointManager` 是唯一经过 `ApplyConfig` 初始化的实例，但它既没有在配置变更后被同步更新，也没有被传递给后续创建的角色。

## 现状分析

### 数据流

```
AppConfig（持久化配置）
  ├── PrimaryModelId          ← 设置页修改
  ├── DefaultModelProviderName ← 设置页修改
  └── Providers[]             ← 设置页修改

ChatRoomService
  ├── _endpointManager（AgentApiEndpointManager）
  │     ├── RegisterLanguageModelProvider()  ← 仅在首次 ApplyConfig 时执行
  │     └── PrimaryModel                     ← 仅在首次 ApplyConfig 时设置，之后不再更新
  │
  └── CreateNewSession()
        └── ChatRoomRole(definition, _endpointManager)  ← 默认角色正确传入
```

### 问题 1：模型切换不生效

**调用链**：
1. 用户在设置页选择新模型 → `SettingsViewModel.SelectedDefaultModel` setter 更新 `_editableCopy.PrimaryModelId`
2. 用户点击返回 → `MainViewModel.NavigateBack()` → `SettingsViewModel.ApplyToConfig()` → `config.PrimaryModelId` 被更新
3. 配置保存到文件
4. **缺失**：没有将新模型同步到 `_endpointManager.PrimaryModel`

`ChatRoomService.ApplyConfig()` 中有 `_isConfigApplied` 守卫，仅首次调用时设置 `PrimaryModel`，后续调用被跳过。

### 问题 2：新角色 EndpointManager 未初始化

**调用链**：
1. `RoleListViewModel.AddRole()` → `new ChatRoomRole(definition)` — 未传入 `endpointManager`
2. `ChatRoomRole` 构造函数中 `endpointManager ?? new AgentApiEndpointManager()` → 创建一个空的 `AgentApiEndpointManager`
3. 该空实例没有注册任何 Provider → `GetSupportedModels()` 返回空 → `PrimaryModel` getter 抛出 `InvalidOperationException`

**对比**：默认角色在 `ChatRoomService.CreateNewSession()` 中创建时正确传入了 `_endpointManager`。

## 修改方案

### 变更 1：`ChatRoomService.cs` — 暴露 EndpointManager + 新增 UpdatePrimaryModel

**文件**：`ChatRoom\Code\ChatRoomAvaloniaDemo\Services\ChatRoomService.cs`

**1a. 暴露 EndpointManager 属性**

在 `_endpointManager` 字段声明后，添加 `internal` 只读属性，供同项目的 `RoleListViewModel` 使用：

```csharp
private readonly AgentApiEndpointManager _endpointManager = new();

/// <summary>
/// 共享的 API 终结点管理器。供同项目的 ViewModel 层在创建角色时传入，
/// 确保所有角色共享同一组已注册的模型提供商。
/// </summary>
internal AgentApiEndpointManager EndpointManager => _endpointManager;
```

**1b. 新增 `UpdatePrimaryModel` 方法**

在 `ApplyConfig` 方法之后，新增公开方法用于配置变更后同步 PrimaryModel：

```csharp
/// <summary>
/// 在配置变更后，同步更新内部 <see cref="AgentApiEndpointManager"/> 的首选模型。
/// 调用方应在保存配置后调用此方法，使模型切换立即生效。
/// </summary>
/// <param name="primaryModelId">新的首选模型 ID 或名称。</param>
public void UpdatePrimaryModel(string primaryModelId)
{
    if (string.IsNullOrWhiteSpace(primaryModelId))
    {
        return;
    }

    ILanguageModel? model = _endpointManager.GetModel(primaryModelId);
    if (model is not null)
    {
        _endpointManager.PrimaryModel = model;
    }
}
```

### 变更 2：`RoleListViewModel.cs` — 新建角色时传入共享 EndpointManager

**文件**：`ChatRoom\Code\ChatRoomAvaloniaDemo\ViewModels\RoleListViewModel.cs`

在 `AddRole()` 方法中，将：

```csharp
var role = new ChatRoomRole(definition);
```

改为：

```csharp
var role = new ChatRoomRole(definition, _chatRoomService.EndpointManager);
```

### 变更 3：`MainViewModel.cs` — NavigateBack 中同步模型

**文件**：`ChatRoom\Code\ChatRoomAvaloniaDemo\ViewModels\MainViewModel.cs`

在 `NavigateBack()` 方法中，`SettingsViewModel.ApplyToConfig()` 之后、保存配置到文件之前，增加模型同步调用：

```csharp
public async void NavigateBack()
{
    if (CurrentView == ChatRoomView.Settings && SettingsViewModel is not null)
    {
        SettingsViewModel.ApplyToConfig();

        // 立即同步模型变更到 ChatRoomService
        var config = _chatRoomService.AppConfig;
        if (!string.IsNullOrWhiteSpace(config?.PrimaryModelId))
        {
            _chatRoomService.UpdatePrimaryModel(config.PrimaryModelId);
        }

        // 保存配置到文件
        if (!string.IsNullOrEmpty(config?.ConfigFilePath))
        {
            try
            {
                await config.SaveAsync(config.ConfigFilePath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存配置失败: {ex.Message}");
            }
        }
    }

    CurrentView = ChatRoomView.ChatRoom;
}
```

## 不需要修改的文件

| 文件 | 原因 |
|------|------|
| `AgentApiEndpointManager.cs` | `PrimaryModel` setter 已支持运行时切换，设置 `_userSetPrimaryLanguageModel` 并清除 `_autoSetPrimaryLanguageModel` |
| `ChatRoomRole.cs` | 构造函数已支持接收外部 `endpointManager` 参数 |
| `SettingsViewModel.cs` | 设置页逻辑正确，`ApplyToConfig()` 正确写回配置 |
| `CopilotChatManager.cs` | 通过共享 `AgentApiEndpointManager` 自动感知变更 |

## 生效路径

```
设置页切换模型：
  SettingsViewModel.ApplyToConfig()
    → config.PrimaryModelId 更新
  MainViewModel.NavigateBack()
    → ChatRoomService.UpdatePrimaryModel(config.PrimaryModelId)
      → _endpointManager.PrimaryModel = newModel
        → 所有共享此 _endpointManager 的 ChatRoomRole
          → CopilotChatManager.AgentApiEndpointManager.PrimaryModel
            → GetChatClientAsync() 使用新模型 ✓

新建角色：
  RoleListViewModel.AddRole()
    → new ChatRoomRole(definition, _chatRoomService.EndpointManager)
      → ChatRoomRole._endpointManager = 共享实例（已注册所有 Provider）
        → CopilotChatManager.AgentApiEndpointManager
          → PrimaryModel 可正常获取 ✓
```

## 风险与边界

- **Provider 动态变更**：当前方案仅同步 PrimaryModel。如果用户在设置中新增/修改/删除了 Provider，`_endpointManager` 中已注册的 Provider 不会自动更新。这属于更大范围的改动，不在本次修复范围内。
- **并发安全**：`PrimaryModel` setter 无锁保护，但 UI 场景下单线程操作，无实际并发风险。
- **`internal` 可见性**：`EndpointManager` 使用 `internal` 修饰符，`RoleListViewModel` 与 `ChatRoomService` 在同一程序集（`ChatRoomAvaloniaDemo`）中，无需 `InternalsVisibleTo`。
