# ChatRoom 角色大厅功能设计计划

## 背景

当前 `ChatRoom` 应用的角色管理是**会话级**的：每个角色定义（`ChatRoomRoleDefinition`）绑定在具体的 `ChatRoomSession` 中，存储于 `ChatRoomSessionData.Roles` 列表。用户每次新建会话后需要重新配置角色，无法跨会话复用已有的角色设定。

实际使用中，用户经常有"一组常用角色模板"的需求，例如：
- "架构师"——关注系统设计和技术选型
- "代码审查员"——关注代码质量和最佳实践
- "测试工程师"——关注边界条件和测试覆盖

角色大厅（Role Lobby）提供一个**全局角色模板库**，让用户可以：
1. 浏览、搜索预置和自定义的角色模板
2. 一键将模板中的角色添加到当前会话
3. 将当前会话中的角色提升到角色大厅，供未来复用
4. 对模板进行编辑、分类和删除

> **约束**：本计划仅输出设计文档，不修改任何代码。后续实施时按本文档执行。

## 架构总览

```
ChatRoom.AvaloniaShell（UI 层）
├── ViewModels/
│   ├── MainViewModel                ← 新增 AppPage.RoleLobby 导航
│   ├── RoleLobbyViewModel           ← 新增：角色大厅 ViewModel
│   └── RoleListViewModel            ← 新增"从大厅添加"入口
├── Views/
│   └── RoleLobbyView.axaml          ← 新增：角色大厅视图
│
AgentLib.ChatRoom（核心库）
├── Model/
│   ├── RoleTemplate.cs              ← 新增：角色模板数据模型
│   └── RoleTemplateCategory.cs      ← 新增：模板分类枚举
├── Services/
│   └── RoleTemplateService.cs       ← 新增：模板 CRUD + 持久化
└── ChatRoomPersistence.cs           ← 不改动，模板独立持久化
```

## 核心设计决策

### 决策 1：模板模型 vs 角色定义

角色大厅引入 `RoleTemplate` 模型，而非直接复用 `ChatRoomRoleDefinition`。

**理由**：

| 维度 | 直接复用 `ChatRoomRoleDefinition` | 新增 `RoleTemplate`（采用） |
|------|-----------------------------------|----------------------------|
| 扩展性 | 无法添加模板特有字段 | 可添加分类、标签、创建时间等 |
| 关注点分离 | 模板和会话角色混为一谈 | 模板是"蓝图"，角色定义是"实例" |
| 序列化独立性 | 需要嵌入会话数据结构 | 独立文件，不依赖会话 |
| 向后兼容 | 修改 `ChatRoomRoleDefinition` 影响现有会话 | 零影响 |

`RoleTemplate` 设计如下：

```csharp
public sealed class RoleTemplate
{
    /// <summary>模板唯一标识。</summary>
    public string TemplateId { get; init; } = string.Empty;

    /// <summary>模板显示名。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>模板描述（一句话说明角色用途）。</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>分类（如"开发"、"产品"、"通用"）。</summary>
    public string Category { get; set; } = "通用";

    /// <summary>标签列表（用于搜索和筛选）。</summary>
    public List<string> Tags { get; init; } = [];

    /// <summary>创建时间。</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>最后修改时间。</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>对应的角色定义（完整复用 ChatRoomRoleDefinition 结构）。</summary>
    public ChatRoomRoleDefinition Definition { get; set; } = new();
}
```

**关键设计**：`RoleTemplate.Definition` 复用 `ChatRoomRoleDefinition`，确保从模板创建角色时属性完整对齐。添加到会话时，生成新的 `RoleId`（避免与已有角色冲突），其余属性从模板复制。

### 决策 2：持久化方式 —— 独立 JSON 文件

角色模板存储在 `{AppData}/AgentRoundtable/RoleTemplates/` 目录下，每个模板一个 JSON 文件。

**文件结构**：

```
{AppData}/AgentRoundtable/
├── AppConfiguration.json          ← 现有：全局设置
├── Sessions/                      ← 现有：会话持久化
│   └── {sessionId}/
│       └── room.config.json
└── RoleTemplates/                 ← 新增：模板持久化
    ├── {templateId}.json          ← 每个模板一个文件
    └── {templateId}.json
```

**理由**：
- 与会话持久化完全解耦，删除会话不影响模板
- 每个模板独立文件，便于后续扩展为导入/导出单个模板
- 复用现有的 `System.Text.Json` + `JsonSerializerContext` 序列化方案
- 路径通过 `AppSettings` 可配置（新增 `RoleTemplatesPath` 属性），默认回退到 `{AppData}/AgentRoundtable/RoleTemplates`

### 决策 3：UI 集成 —— 作为中栏独立页面

角色大厅作为 `AppPage.RoleLobby` 加入现有导航体系，在中栏（Grid.Column="2"）全屏展示，替代三栏中的聊天/设置/角色编辑区域。

**入口点**（详见"界面设计"章节）：

1. **右栏角色列表顶栏新增"大厅"按钮** — 与现有"+ 添加"按钮并列，点击后中栏切换到角色大厅页面
2. **左栏会话列表底部新增"角色大厅"按钮** — 与现有"⚙ 设置"按钮垂直排列
3. **右栏角色项右键菜单新增"提升到角色大厅"** — 将当前会话角色提升为全局模板

**理由**：
- 复用现有 `AppPage` 导航机制，无需改动三栏布局结构
- 大厅需要较大的展示空间（卡片网格），三栏布局中仅中栏有足够宽度
- 进入大厅时左栏和右栏保持可见，用户始终知道当前操作的会话上下文
- 从右栏角色列表入口进入，符合用户"添加角色"的操作动线
- 右键菜单提供"提升到角色大厅"快捷操作，符合用户从已有角色创建模板的自然动线

### 决策 4：大厅交互流程

```用户通过三个入口进入角色大厅
    │
    ├── 入口 1: 右栏角色列表顶栏"大厅"按钮
    ├── 入口 2: 左栏会话列表底部"角色大厅"按钮
    └── 入口 3: 右栏角色项右键"提升到角色大厅"
    │
    ▼
角色大厅页面（卡片网格 + 搜索栏 + 分类筛选）
    │
    ├── 点击卡片"添加到当前会话"
    │       │
    │       ▼
    │   从模板创建 ChatRoomRoleDefinition（生成新 RoleId）
    │       │
    │       ▼
    │   ChatRoomService.AddRoleAsync(definition)
    │       │
    │       ▼
    │   导航回聊天页面，右栏角色列表自动刷新
    │
    ├── 点击卡片"编辑模板"
    │       │
    │       ▼
    │   打开模板编辑器（复用 RoleEditView 或新建 TemplateEditView）
    │
    ├── 点击顶栏"+ 提升当前角色到大厅"或右键菜单"提升到角色大厅"
    │       │
    │       ▼
    │   大厅顶部展开内联保存表单
    │       │  选择来源角色（从当前会话角色列表中选）
    │       │  填写模板名/描述/分类/标签
    │       ▼
    │   RoleTemplateService.SaveAsync(template)
    │       │  → 写入 {RoleTemplatesPath}/{templateId}.json
    │       │  → 刷新卡片列表，新模板出现
    │       │  → 表单收起
    │
    └── 点击卡片"删除模板"
            │
            ▼
        确认后从持久化删除
```

### 决策 5：预置模板

首次启动时，如果模板目录为空，自动写入一批预置模板。

预置模板列表（示例）：

| 模板名 | 分类 | 系统提示词摘要 | 标签 |
|--------|------|---------------|------|
| 助手 | 通用 | 乐于助人的 AI 助手 | 通用, 默认 |
| 架构师 | 开发 | 关注系统设计、扩展性和技术选型 | 开发, 架构 |
| 代码审查员 | 开发 | 关注代码质量、可维护性和最佳实践 | 开发, 审查 |
| 测试工程师 | 开发 | 关注边界条件、异常路径和测试覆盖 | 开发, 测试 |
| 产品经理 | 产品 | 关注用户需求、功能优先级和 ROI | 产品, 需求 |
| 文档撰写员 | 通用 | 关注清晰、结构化的技术文档 | 通用, 文档 |

预置模板的 `TemplateId` 使用固定常量（如 `"preset_architect"`），便于版本升级时检测和更新。

**理由**：
- 降低首次使用门槛，用户打开大厅即可看到可用角色
- 预置模板可被用户修改或删除，不强制保留
- 固定 ID 便于后续版本迭代时更新预置内容

### 决策 6：模板编辑复用 RoleEditViewModel

模板编辑复用现有的 `RoleEditViewModel`，通过适配层传递模板数据。

**方案**：

1. `RoleEditViewModel` 已支持"新建模式"和"编辑模式"两种状态
2. 新增"模板编辑模式"：构造时传入 `RoleTemplate` 而非 `roleId`
3. 保存时根据模式分别调用 `ChatRoomService.AddRoleAsync`（会话角色）或 `RoleTemplateService.SaveTemplate`（模板）

**替代方案（不采用）**：新建独立的 `TemplateEditViewModel`。理由是模板和会话角色的编辑字段完全一致（RoleName、SystemPrompt、ModelProviderId、ModelId、MemoryContent、ParticipationMode、IsHuman），独立 ViewModel 会产生代码重复。

## 详细设计

### 1. 新增模型：`RoleTemplate`

**文件**：`AgentLib.ChatRoom/Model/RoleTemplate.cs`

```csharp
namespace AgentLib.ChatRoom.Model;

public sealed class RoleTemplate
{
    public string TemplateId { get; init; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "通用";
    public List<string> Tags { get; init; } = [];
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; set; }
    public ChatRoomRoleDefinition Definition { get; set; } = new();
}
```

### 2. 新增服务：`RoleTemplateService`

**文件**：`AgentLib.ChatRoom/Services/RoleTemplateService.cs`

**职责**：
- 从磁盘加载所有模板
- 保存/更新单个模板
- 删除模板
- 首次启动时写入预置模板
- 提供从模板创建 `ChatRoomRoleDefinition` 的转换方法

**关键方法**：

| 方法 | 说明 |
|------|------|
| `IReadOnlyList<RoleTemplate> LoadAll()` | 加载所有模板 |
| `Task SaveAsync(RoleTemplate template)` | 保存或更新模板 |
| `Task DeleteAsync(string templateId)` | 删除模板 |
| `ChatRoomRoleDefinition ToDefinition(RoleTemplate template)` | 从模板生成角色定义（分配新 RoleId） |
| `RoleTemplate FromDefinition(ChatRoomRoleDefinition def, string name, string desc, string category)` | 从会话角色创建模板 |
| `Task EnsurePresetTemplatesAsync()` | 首次启动时写入预置模板 |

**序列化**：使用 `System.Text.Json` + 源生成 `JsonSerializerContext`，与现有 `ChatRoomJsonSerializerContext` 模式一致。新增 `RoleTemplateJsonSerializerContext`。

### 3. 新增 ViewModel：`RoleLobbyViewModel`

**文件**：`ChatRoom.AvaloniaShell/ViewModels/RoleLobbyViewModel.cs`

**属性**：

| 属性 | 类型 | 说明 |
|------|------|------|
| `Templates` | `ObservableCollection<RoleTemplateCardViewModel>` | 模板卡片列表 |
| `SearchText` | `string` | 搜索关键词（匹配名称、描述、标签） |
| `SelectedCategory` | `string?` | 当前选中的分类筛选 |
| `Categories` | `List<string>` | 所有可用分类 |
| `IsBusy` | `bool` | 加载状态（继承自 ViewModelBase） |

**命令**：

| 命令 | 说明 |
|------|------|
| `AddToSessionCommand` | 将模板添加到当前会话 |
| `EditTemplateCommand` | 编辑模板 |
| `DeleteTemplateCommand` | 删除模板 |
| `PromoteToLobbyCommand` | 将当前会话角色提升到角色大厅 |
| `BackCommand` | 返回聊天页面 |

**事件**：

| 事件 | 说明 |
|------|------|
| `BackRequested` | 请求返回上一页面 |
| `EditTemplateRequested` | 请求编辑模板，参数为 TemplateId |

**筛选逻辑**：`SearchText` 变更时，对 `Templates` 执行 `CollectionViewSource` 式过滤（或直接重建集合），匹配 `Name`、`Description`、`Tags` 三个字段。

### 4. 新增 ViewModel：`RoleTemplateCardViewModel`

**文件**：`ChatRoom.AvaloniaShell/ViewModels/RoleLobbyViewModel.cs`（同文件，紧随 `RoleLobbyViewModel`）

封装单个模板卡片的显示数据：

| 属性 | 说明 |
|------|------|
| `TemplateId` | 模板 ID |
| `Name` | 模板名 |
| `Description` | 描述 |
| `Category` | 分类 |
| `TagsDisplay` | 标签显示文本（如 "开发 · 审查"） |
| `Initial` | 名称首字（头像显示） |
| `SystemPromptSummary` | 系统提示词摘要（前 50 字） |
| `ModelDisplay` | 模型显示文本 |
| `IsPreset` | 是否预置模板（影响删除确认文案） |

### 5. 新增视图：`RoleLobbyView.axaml`

**文件**：`ChatRoom.AvaloniaShell/Views/RoleLobbyView.axaml`

**布局结构**：

```
┌──────────────────────────────────────────────────┐
│  ← 返回    角色大厅                  + 保存当前角色  │  ← 顶栏
├──────────────────────────────────────────────────┤
│  [搜索框]                    [分类: 全部 ▾]       │  ← 筛选栏
├──────────────────────────────────────────────────┤
│  ┌─────────┐  ┌─────────┐  ┌─────────┐          │
│  │  头像    │  │  头像    │  │  头像    │          │
│  │ 架构师   │  │ 审查员   │  │ 测试     │          │  ← 卡片网格
│  │ 描述...  │  │ 描述...  │  │ 描述...  │    （WrapPanel
│  │ 开发·架构│  │ 开发·审查│  │ 开发·测试│     或 ItemsControl
│  │ [+添加]  │  │ [+添加]  │  │ [+添加]  │     + UniformGrid）
│  │ [✏编辑]  │  │ [✏编辑]  │  │ [✏编辑]  │
│  │ [🗑删除]  │  │ [🗑删除]  │  │ [🗑删除]  │
│  └─────────┘  └─────────┘  └─────────┘          │
│                                                  │
│  ┌─────────┐  ┌─────────┐                       │
│  │  ...     │  │  ...     │                       │
│  └─────────┘  └─────────┘                       │
└──────────────────────────────────────────────────┘
```

**样式复用**：
- 卡片样式复用现有 `Card` 样式（定义在 `Controls.axaml`）
- 头像样式复用 `RoleListView` 中的 `Avatar` 样式 + `RoleColorConverter` + `RoleInitialConverter`
- 按钮样式复用 `Primary` / `Flat` / `Danger` 类
- 颜色资源复用 `Colors.axaml` 中定义的全部 `SolidColorBrush`

### 6. 修改 `MainViewModel` 导航

**文件**：`ChatRoom.AvaloniaShell/ViewModels/MainViewModel.cs`

变更内容：

1. `AppPage` 枚举新增 `RoleLobby` 成员
2. 新增 `IsRoleLobbyPage` 布尔属性
3. 新增 `RoleLobbyViewModel? RoleLobbyViewModel` 属性
4. 新增 `NavigateToRoleLobby()` 方法 —— 导航到大厅，展示正常网格视图
5. 新增 `NavigateToRoleLobbyForPromote(string roleId)` 方法 —— 导航到大厅并通知展开内联提升表单、预选指定角色
6. 新增 `OnRoleLobbyBack()` 事件处理 —— 返回聊天页面
7. `RaisePageChanged()` 中追加 `OnPropertyChanged(nameof(IsRoleLobbyPage))`
8. 构造函数中创建 `RoleLobbyViewModel` 实例（注入 `RoleTemplateService` 和 `ChatRoomService`），并订阅其 `BackRequested` 事件

### 7. 修改 `MainView.axaml` 布局

**文件**：`ChatRoom.AvaloniaShell/Views/MainView.axaml`

在中栏 `Grid` 内新增一个 `Panel`：

```xml
<!-- 角色大厅页面 -->
<Panel IsVisible="{Binding IsRoleLobbyPage}">
    <views:RoleLobbyView DataContext="{Binding RoleLobbyViewModel}" />
</Panel>
```

### 8. 修改 `RoleListViewModel` 入口

**文件**：`ChatRoom.AvaloniaShell/ViewModels/RoleListViewModel.cs`

新增"大厅"命令和"提升到角色大厅"命令及对应事件：

```csharp
/// <summary>打开角色大厅命令。</summary>
public ICommand OpenLobbyCommand { get; }

/// <summary>提升角色到大厅命令。参数为角色项 ViewModel。</summary>
public ICommand PromoteToLobbyCommand { get; }

/// <summary>打开角色大厅请求事件。</summary>
public event EventHandler? OpenLobbyRequested;

/// <summary>提升角色到大厅请求事件。参数为角色 ID。</summary>
public event EventHandler<string>? PromoteToLobbyRequested;
```

在构造函数中初始化：

```csharp
OpenLobbyCommand = new SimpleCommand(() => OpenLobbyRequested?.Invoke(this, EventArgs.Empty));
PromoteToLobbyCommand = new SimpleCommand<RoleItemViewModel>(role => PromoteToLobbyRequested?.Invoke(this, role.RoleId));
```

`MainViewModel` 构造函数中订阅：

```csharp
RoleListViewModel.OpenLobbyRequested += (s, e) => NavigateToRoleLobby();
RoleListViewModel.PromoteToLobbyRequested += (s, roleId) => NavigateToRoleLobbyForPromote(roleId);
```

`NavigateToRoleLobbyForPromote` 方法导航到角色大厅并通知 `RoleLobbyViewModel` 展开内联提升表单、预选指定角色。

### 9. 修改 `SessionListViewModel` 入口

**文件**：`ChatRoom.AvaloniaShell/ViewModels/SessionListViewModel.cs`

在底部栏"设置"按钮旁新增"角色大厅"按钮：

```csharp
public ICommand OpenLobbyCommand { get; }
public event EventHandler? OpenLobbyRequested;
```

### 10. 修改 `AppSettings` 配置

**文件**：`AgentLib.ChatRoom/AppSettings/AppSettings.cs`

新增可选配置项：

```csharp
/// <summary>
/// 角色模板持久化路径。为空时使用默认路径 {AppData}/AgentRoundtable/RoleTemplates。
/// </summary>
public string? RoleTemplatesPath { get; set; }
```

### 11. 修改 `App.axaml.cs` 初始化

**文件**：`ChatRoom.AvaloniaShell/App.axaml.cs`

在 `InitializeApp` 方法中：

1. 解析模板持久化路径（优先使用 `AppSettings.RoleTemplatesPath`，回退到默认路径）
2. 创建 `RoleTemplateService` 实例
3. 调用 `EnsurePresetTemplatesAsync()` 写入预置模板（仅首次）
4. 将 `RoleTemplateService` 传递给 `MainViewModel` 构造函数

## 需要新增的文件

| 序号 | 文件路径 | 说明 |
|------|---------|------|
| 1 | `AgentLib.ChatRoom/Model/RoleTemplate.cs` | 角色模板数据模型 |
| 2 | `AgentLib.ChatRoom/Model/RoleTemplateJsonSerializerContext.cs` | 模板 JSON 序列化上下文 |
| 3 | `AgentLib.ChatRoom/Services/RoleTemplateService.cs` | 模板管理服务 |
| 4 | `AgentLib.ChatRoom/Services/PresetTemplates.cs` | 预置模板定义 |
| 5 | `ChatRoom.AvaloniaShell/ViewModels/RoleLobbyViewModel.cs` | 大厅 ViewModel + 卡片 ViewModel + 内联保存表单 ViewModel |
| 6 | `ChatRoom.AvaloniaShell/Views/RoleLobbyView.axaml` | 大厅视图（含顶栏、筛选栏、卡片网格、内联保存表单、空状态） |
| 7 | `ChatRoom.AvaloniaShell/Views/RoleLobbyView.axaml.cs` | 大厅视图 code-behind |

## 需要修改的现有文件

| 序号 | 文件路径 | 变更内容 |
|------|---------|---------|
| 1 | `AgentLib.ChatRoom/AppSettings/AppSettings.cs` | 新增 `RoleTemplatesPath` 属性 |
| 2 | `ChatRoom.AvaloniaShell/ViewModels/MainViewModel.cs` | 新增 `AppPage.RoleLobby` 导航 |
| 3 | `ChatRoom.AvaloniaShell/Views/MainView.axaml` | 中栏新增 `RoleLobbyView` 面板 |
| 4 | `ChatRoom.AvaloniaShell/ViewModels/RoleListViewModel.cs` | 新增"大厅"入口命令 + "提升到角色大厅"命令 + 角色项右键菜单事件 |
| 5 | `ChatRoom.AvaloniaShell/ViewModels/SessionListViewModel.cs` | 底部栏新增"角色大厅"入口命令和事件 |
| 6 | `ChatRoom.AvaloniaShell/Views/SessionListView.axaml` | 底部栏新增"角色大厅"按钮（与"设置"按钮垂直排列） |
| 7 | `ChatRoom.AvaloniaShell/Views/RoleListView.axaml` | 顶栏新增"大厅"按钮 + 角色项新增右键上下文菜单（含"提升到角色大厅"） |
| 8 | `ChatRoom.AvaloniaShell/App.axaml.cs` | 初始化 `RoleTemplateService` |

## 数据流

### 从模板添加角色到会话

```
RoleLobbyView
    │  用户点击"添加到当前会话"
    ▼
RoleLobbyViewModel.AddToSessionCommand
    │  调用 RoleTemplateService.ToDefinition(template)
    │  → 生成新 RoleId，复制 Definition 属性
    ▼
ChatRoomService.AddRoleAsync(definition)
    │  内部调用 ChatRoomManager.AddRoleAsync
    │  → 角色加入 Roles 集合
    │  → RoleListViewModel 通过 SessionChanged 事件自动刷新
    ▼
MainViewModel.NavigateToChat()
    │  返回聊天页面
    ▼
右栏角色列表显示新添加的角色
```

### 从会话角色提升到角色大厅

```
RoleLobbyView
    │  用户点击"保存当前角色为模板"
    ▼
弹出对话框（或内联表单）
    │  选择当前会话中的角色
    │  填写模板名、描述、分类
    ▼
RoleTemplateService.FromDefinition(role.Definition, name, desc, category)
    │  → 生成新 TemplateId
    │  → 复制 Definition（保留原 RoleId 或生成新的）
    ▼
RoleTemplateService.SaveAsync(template)
    │  → 序列化为 JSON 写入 {RoleTemplatesPath}/{templateId}.json
    ▼
RoleLobbyViewModel.RefreshTemplates()
    │  → 重新加载模板列表
    │  → 新模板出现在卡片网格中
```

## 界面设计

本章节详细描述角色大厅在现有三栏布局中的位置、入口设计、各页面的布局结构以及交互细节。

### 现有界面布局回顾

当前主界面为三栏式布局（`MainView.axaml`）：

```
┌──────────┬─────────────────────────────────┬──────────┐
│          │                                 │          │
│  左栏     │           中栏                   │  右栏     │
│  250px   │           自适应                  │  280px   │
│          │                                 │          │
│ 会话列表  │  聊天 / 设置 / 角色编辑（切换）    │ 角色列表  │
│          │                                 │          │
│          │                                 │ + 添加   │
│          │                                 │ ✏ 编辑   │
│          │                                 │ 🗑 删除   │
│          │                                 │          │
├──────────┤                                 │          │
│ ⚙ 设置   │                                 │          │
└──────────┴─────────────────────────────────┴──────────┘
```

- **左栏**（`SessionListView`）：会话列表 + 底部"设置"按钮
- **中栏**：通过 `AppPage` 枚举切换显示聊天/设置/角色编辑页面
- **右栏**（`RoleListView`）：当前会话的角色列表 + 添加/编辑/删除按钮

### 入口设计：角色大厅在现有界面中的位置

角色大厅作为中栏的一个新页面（`AppPage.RoleLobby`），与聊天页面、设置页面、角色编辑页面同级切换。进入大厅时，左栏和右栏保持不变，仅中栏内容替换为大厅视图。

**入口点共有三处**，分布在左栏和右栏：

#### 入口 1：右栏角色列表顶栏 —— "大厅"按钮

在 `RoleListView.axaml` 的标题栏中，现有"+ 添加"按钮左侧新增"大厅"按钮。

现有布局：

```
┌──────────────────────────┐
│  角色列表        [+ 添加]  │
└──────────────────────────┘
```

修改后：

```
┌──────────────────────────┐
│  角色列表    [大厅] [+ 添加] │
└──────────────────────────┘
```

此入口的动线逻辑：用户在右栏看到当前会话的角色列表 → 想从模板添加角色 → 点击"大厅" → 中栏切换到角色大厅页面 → 选择模板添加 → 自动返回聊天页面。

#### 入口 2：左栏会话列表底部 —— "角色大厅"按钮

在 `SessionListView.axaml` 的底部栏，现有"⚙ 设置"按钮上方新增"角色大厅"按钮。

现有布局：

```
┌──────────────────────────┐
│  会话列表         [+ 新建]  │
│                          │
│  ...会话项...              │
│                          │
├──────────────────────────┤
│  ⚙ 设置                   │
└──────────────────────────┘
```

修改后：

```
┌──────────────────────────┐
│  会话列表         [+ 新建]  │
│                          │
│  ...会话项...              │
│                          │
├──────────────────────────┤
│  👥 角色大厅               │
│  ⚙ 设置                   │
└──────────────────────────┘
```

底部栏由单按钮变为双按钮垂直排列。此入口的动线逻辑：用户从全局视角管理角色模板，不依赖当前会话。

#### 入口 3：右栏角色项右键菜单 —— "提升到角色大厅"

在 `RoleListView.axaml` 的角色项上新增右键上下文菜单，提供"保存到角色大厅"选项。此入口用于将当前会话中的角色提升为全局模板。

现有角色项无右键菜单。修改后角色项结构：

```
┌──────────────────────────────┐
│  [头像]  架构师    [✏] [🗑]    │
│         开发 · 架构            │
│         关注系统设计...        │   ← 右键点击弹出菜单
│         gemini-2.5-pro        │
└──────────────────────────────┘
                     ┌───────────────────┐
                     │  📋 提升到角色大厅  │
                     │  ✏  编辑角色      │
                     │  🗑  删除角色     │
                     └───────────────────┘
```

### 在聊天界面上添加角色的方式

当用户在聊天页面中想要添加角色时，有两条路径：

**路径 A：从角色大厅添加（推荐）**

```
聊天页面
  │  用户看到右栏角色列表顶部的"大厅"按钮
  ▼
点击"大厅"
  │  中栏切换到角色大厅页面
  │  左栏（会话列表）和右栏（当前角色列表）保持可见
  ▼
角色大厅页面
  │  浏览/搜索模板卡片
  │  点击某卡片上的"添加到当前会话"按钮
  ▼
角色被添加到当前会话
  │  右栏角色列表实时刷新（通过 SessionChanged 事件）
  ▼
自动返回聊天页面
  │  新角色出现在右栏列表中，可立即参与对话
```

**路径 B：直接新建角色（现有方式，保持不变）**

```
聊天页面
  │  点击右栏角色列表顶部的"+ 添加"按钮
  ▼
中栏切换到角色编辑页面（新建模式）
  │  手动填写角色名、系统提示词、模型等
  ▼
保存后返回聊天页面
```

两条路径并存：路径 A 适合从已有模板快速添加，路径 B 适合创建一次性自定义角色。

### 将会话角色提升到角色大厅的方式

用户在右栏角色列表中，对某个角色项执行以下操作即可将其保存为全局模板：

```
右栏角色列表
  │  右键点击某个角色项
  ▼
弹出上下文菜单
  │  选择"提升到角色大厅"
  ▼
中栏切换到角色大厅页面，顶部显示内联保存表单
  │  ┌──────────────────────────────────────┐
  │  │  保存角色到大厅                       │
  │  │                                      │
  │  │  模板名称: [架构师          ]          │
  │  │  描述:     [关注系统设计...  ]          │
  │  │  分类:     [开发          ▾]          │
  │  │  标签:     [架构, 系统设计   ]          │
  │  │                                      │
  │  │  [取消]              [保存模板]        │
  │  └──────────────────────────────────────┘
  │
  │  表单预填充：模板名=角色名，描述=系统提示词前 50 字
  │  分类默认"通用"，用户可修改
  ▼
点击"保存模板"
  │  RoleTemplateService.SaveAsync() 写入磁盘
  │  大厅卡片列表刷新，新模板出现在网格中
  │  顶部表单收起，显示正常的大厅网格视图
```

**设计要点**：
- 保存表单以**内联面板**形式出现在大厅页面顶部，而非弹窗对话框，保持与现有设置页/角色编辑页一致的页面内交互风格
- 表单字段预填充角色已有信息，降低用户输入成本
- 保存后不自动返回聊天页面，而是停留在大厅让用户确认结果，用户手动点击"返回"回到聊天

### 角色大厅页面布局

角色大厅页面占据中栏全部空间，从上到下分为三个区域：

```
┌──────────────────────────────────────────────────────────┐
│  ← 返回       角色大厅                  + 提升当前角色到大厅  │  ① 顶栏
├──────────────────────────────────────────────────────────┤
│  🔍 [搜索模板...]                    分类: [全部 ▾]        │  ② 筛选栏
├──────────────────────────────────────────────────────────┤
│                                                          │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐               │
│  │  [头像]   │  │  [头像]   │  │  [头像]   │               │
│  │  架构师   │  │  审查员   │  │  测试     │               │
│  │          │  │          │  │          │               │  ③ 卡片网格
│  │ 关注系统  │  │ 关注代码  │  │ 关注边界  │               │
│  │ 设计...   │  │ 质量...   │  │ 条件...   │               │
│  │          │  │          │  │          │               │
│  │ 开发·架构 │  │ 开发·审查 │  │ 开发·测试 │               │
│  │          │  │          │  │          │               │
│  │ gemini-  │  │ 默认模型  │  │ 默认模型  │               │
│  │ 2.5-pro  │  │          │  │          │               │
│  │          │  │          │  │          │               │
│  │ [+ 添加]  │  │ [+ 添加]  │  │ [+ 添加]  │               │
│  │ [✏ 编辑]  │  │ [✏ 编辑]  │  │ [✏ 编辑]  │               │
│  │ [🗑 删除]  │  │ [🗑 删除]  │  │ [🗑 删除]  │               │
│  └──────────┘  └──────────┘  └──────────┘               │
│                                                          │
│  ┌──────────┐  ┌──────────┐                             │
│  │  ...      │  │  ...      │                             │
│  └──────────┘  └──────────┘                             │
│                                                          │
└──────────────────────────────────────────────────────────┘
```

#### ① 顶栏

| 元素 | 样式 | 说明 |
|------|------|------|
| "← 返回"按钮 | `Button.Flat` | 返回上一个页面（聊天或角色编辑） |
| "角色大厅"标题 | `TextBlock.Title` | 居中显示 |
| "+ 提升当前角色到大厅"按钮 | `Button.Primary` | 打开内联提升表单，需有活跃会话时可用 |

顶栏结构与 `RoleEditView` 的标题栏一致：`← 返回` 居左，标题居中，操作按钮居右。

#### ② 筛选栏

| 元素 | 说明 |
|------|------|
| 搜索框 | `TextBox.Rounded`，Watermark="搜索模板..."，实时过滤名称/描述/标签 |
| 分类下拉 | `ComboBox`，选项动态生成自所有模板的 `Category` 字段去重，首位为"全部" |

搜索逻辑：不区分大小写，匹配 `Name`、`Description`、`Tags` 三个字段的子串。`SearchText` 变更时立即重新过滤，无需点击搜索按钮。

分类筛选：选择"全部"时显示所有模板；选择具体分类时仅显示该分类下的模板。搜索和分类可组合使用。

#### ③ 卡片网格

使用 `ItemsControl` + `WrapPanel`（或 `UniformGrid` 自适应列数）展示模板卡片。

每张卡片结构：

```
┌────────────────────┐
│      ┌──────┐      │
│      │ 头像  │      │  ← 圆形头像，Background 使用 RoleColorConverter
│      │  架   │      │     Text 使用 RoleInitialConverter
│      └──────┘      │
│                    │
│     架构师          │  ← 模板名，FontWeight=Bold，FontSize=14
│                    │
│  关注系统设计、      │  ← 描述，FontSize=12，TextWrapping=Wrap
│  扩展性和技术选型    │     最多显示 2 行，超出省略
│                    │
│   开发 · 架构       │  ← 分类 + 标签，Subtitle 样式
│                    │
│   gemini-2.5-pro   │  ← 模型显示，Subtitle 样式
│                    │
│  [+ 添加到当前会话]  │  ← Primary 按钮，全宽
│  [✏ 编辑]  [🗑 删除] │  ← Flat / Danger 按钮，一行两个
└────────────────────┘
```

**卡片样式**：
- 外层 `Border` 使用 `Card` 样式（`Background=SurfaceBrush`，`CornerRadius=8`）
- 固定宽度约 200px，高度自适应内容
- 内边距 `Padding="16,12"`，外边距 `Margin="8"`
- 头像复用 `RoleListView` 中的 `Avatar` 样式 + `RoleColorConverter` + `RoleInitialConverter`

**按钮状态**：
- "添加到当前会话"按钮：当 `ChatRoomService.HasActiveSession` 为 `false` 时禁用，并显示提示文本"请先打开一个会话"
- "删除"按钮：预置模板（`IsPreset=true`）删除时弹出确认对话框，自定义模板可直接删除或轻确认

### 进入角色大厅时左栏和右栏的状态

进入角色大厅时，三栏布局的左右两栏保持可见但进入"只读"状态：

| 栏 | 状态 | 说明 |
|----|------|------|
| 左栏（会话列表） | 可见，可操作 | 用户可以切换会话，切换后右栏角色列表同步更新，大厅中"添加到当前会话"按钮的目标会话也随之变化 |
| 中栏 | 大厅页面 | 卡片网格 + 搜索筛选 |
| 右栏（角色列表） | 可见，可操作 | 用户可以看到当前会话已有角色，也可以直接从右栏添加/编辑/删除角色 |

**理由**：保持左右栏可见让用户始终知道当前操作的会话上下文，也方便从右栏直接跳转到大厅（通过"大厅"按钮）再返回。

### 空状态设计

当模板列表为空（用户删除了所有模板）或搜索/筛选无结果时，卡片网格区域显示空状态提示：

```
┌──────────────────────────────────────────────────┐
│                                                  │
│                                                  │
│                   📭                              │
│                                                  │
│              暂无角色模板                          │
│                                                  │
│         你可以从当前会话中的角色创建模板            │
│         点击右上角"提升当前角色到大厅"              │
│                                                  │
│                                                  │
└──────────────────────────────────────────────────┘
```

空状态使用 `TextBlock` 居中显示，`FontSize=16`，`Foreground=TextSecondaryBrush`，附带引导操作说明。

### 内联保存表单布局

当用户从右栏右键菜单选择"提升到角色大厅"或点击大厅顶栏的"+ 提升当前角色到大厅"按钮时，大厅页面顶部展开内联表单：

```
┌──────────────────────────────────────────────────────────┐
│  ← 返回       角色大厅                  + 提升当前角色到大厅  │
├──────────────────────────────────────────────────────────┤
│  ┌────────────────────────────────────────────────────┐  │
│  提升角色到大厅                                      │  │
│  │                                                    │  │
│  来源角色: [选择角色 ▾]                              │  │
│  │  模板名称: [                                ]        │  │
│  │  描述:     [                                ]        │  │
│  │  分类:     [通用          ▾]    标签: [架构, 系统]    │  │
│  │                                                    │  │
│  │                              [取消]    [提升到大厅]   │  │
│  └────────────────────────────────────────────────────┘  │
├──────────────────────────────────────────────────────────┤
│  🔍 [搜索模板...]                    分类: [全部 ▾]        │
├──────────────────────────────────────────────────────────┤
│  ... 卡片网格 ...                                      │
└──────────────────────────────────────────────────────────┘
```

**表单元素**：

| 字段 | 控件 | 说明 |
|------|------|------|
| 来源角色 | `ComboBox` | 从当前会话角色列表中选择；从右键菜单进入时预选对应角色 |
| 模板名称 | `TextBox.Rounded` | 预填充角色名 |
| 描述 | `TextBox.Rounded` | 预填充系统提示词前 50 字 |
| 分类 | `ComboBox` | 可选择已有分类或输入新分类 |
| 标签 | `TextBox.Rounded` | 逗号分隔输入，如"架构, 系统设计" |
| 取消 | `Button.Flat` | 收起表单，回到正常大厅视图 |
| 提升到大厅 | `Button.Primary` | 验证必填项后保存，收起表单，刷新卡片列表 |

表单外层使用 `Border.Card` 样式，与卡片网格视觉一致，展开/收起带简单过渡动画。

### 导航流程图

```
                        ┌─────────────┐
                        │  聊天页面    │ ←── 默认中栏页面
                        │  (AppPage   │
                        │   .Chat)    │
                        └──────┬──────┘
                               │
              ┌────────────────┼────────────────┐
              │                │                │
     右栏"+ 添加"      右栏"大厅"按钮       右栏右键"提升到角色大厅"
              │                │                │
              ▼                ▼                ▼
      ┌─────────────┐  ┌─────────────┐  ┌─────────────┐
      │  角色编辑    │  │  角色大厅    │  │  角色大厅    │
      │  页面        │  │  页面        │  │  页面        │
      │ (AppPage    │  │ (AppPage    │  │ (AppPage    │
      │  .RoleEdit) │  │  .RoleLobby)│  │  .RoleLobby)│
      └──────┬──────┘  └──────┬──────┘  │  + 内联表单  │
             │                │         └──────┬──────┘
     保存/取消完成        添加角色完成      保存模板完成
             │                │                │
             └────────────────┼────────────────┘
                              ▼
                        ┌─────────────┐
                        │  聊天页面    │
                        │  (AppPage   │
                        │   .Chat)    │
                        └─────────────┘

     左栏"角色大厅"按钮 ──→ 角色大厅页面 ──→ "← 返回" ──→ 聊天页面
```

### 各入口按钮的 AXAML 变更概要

#### `RoleListView.axaml` — 顶栏新增"大厅"按钮

现有标题栏：
```xml
<Grid Grid.Row="0" ColumnDefinitions="*,Auto" Margin="12,10">
    <TextBlock Text="角色列表" Classes="Title" />
    <Button Grid.Column="1" Classes="Primary" Content="+ 添加"
            Command="{Binding AddRoleCommand}" />
</Grid>
```

修改后（列定义从 `*,Auto` 改为 `*,Auto,Auto`）：
```xml
<Grid Grid.Row="0" ColumnDefinitions="*,Auto,Auto" Margin="12,10">
    <TextBlock Text="角色列表" Classes="Title" />
    <Button Grid.Column="1" Classes="Flat" Content="大厅"
            Command="{Binding OpenLobbyCommand}" Margin="0,0,4,0" />
    <Button Grid.Column="2" Classes="Primary" Content="+ 添加"
            Command="{Binding AddRoleCommand}" />
</Grid>
```

角色项新增右键菜单：
```xml
<Border.ContextMenu>
    <ContextMenu>
        <MenuItem Header="📋 提升到角色大厅"
                  Command="{Binding $parent[ListBox].((vm:RoleListViewModel)DataContext).SaveAsTemplateCommand}"
                  CommandParameter="{Binding}" />
        <MenuItem Header="✏ 编辑角色"
                  Command="{Binding $parent[ListBox].((vm:RoleListViewModel)DataContext).EditRoleCommand}"
                  CommandParameter="{Binding}" />
        <MenuItem Header="🗑 删除角色"
                  Command="{Binding $parent[ListBox].((vm:RoleListViewModel)DataContext).DeleteRoleCommand}"
                  CommandParameter="{Binding}" />
    </ContextMenu>
</Border.ContextMenu>
```

#### `SessionListView.axaml` — 底部栏新增"角色大厅"按钮

现有底部栏：
```xml
<Border Grid.Row="2" Padding="12,8" Background="{StaticResource SurfaceBrush}"
        BorderBrush="{StaticResource BorderBrush}" BorderThickness="0,1,0,0">
    <Button Classes="Flat" Content="⚙ 设置"
            Command="{Binding OpenSettingsCommand}"
            HorizontalAlignment="Stretch" HorizontalContentAlignment="Left" />
</Border>
```

修改后（改为 `StackPanel` 包裹两个按钮）：
```xml
<Border Grid.Row="2" Padding="12,8" Background="{StaticResource SurfaceBrush}"
        BorderBrush="{StaticResource BorderBrush}" BorderThickness="0,1,0,0">
    <StackPanel Spacing="4">
        <Button Classes="Flat" Content="👥 角色大厅"
                Command="{Binding OpenLobbyCommand}"
                HorizontalAlignment="Stretch" HorizontalContentAlignment="Left" />
        <Button Classes="Flat" Content="⚙ 设置"
                Command="{Binding OpenSettingsCommand}"
                HorizontalAlignment="Stretch" HorizontalContentAlignment="Left" />
    </StackPanel>
</Border>
```

## 边界条件与风险

### 1. 当前无活跃会话时添加角色

**场景**：用户在角色大厅点击"添加到当前会话"，但当前没有打开任何会话。

**处理**：禁用"添加到当前会话"按钮，显示提示文本"请先创建或打开一个会话"。通过 `ChatRoomService.HasActiveSession` 判断。

### 2. 模板文件损坏

**场景**：模板 JSON 文件被手动修改导致反序列化失败。

**处理**：`RoleTemplateService.LoadAll()` 对每个文件使用 `try-catch` 包裹，损坏文件跳过并记录警告日志，不影响其他模板加载。

### 3. 模板名称重复

**场景**：用户保存的模板名称与已有模板重复。

**处理**：允许重名（`TemplateId` 是唯一标识），但在卡片上显示时如有重名则追加序号后缀。不做强制唯一性校验，降低使用摩擦。

### 4. 预置模板被用户删除后升级

**场景**：用户删除了预置模板，后续版本升级时 `EnsurePresetTemplatesAsync` 是否重新写入？

**处理**：`EnsurePresetTemplatesAsync` 仅在模板目录完全为空时执行。用户删除预置模板后，目录非空，不会重新写入。版本升级时如需更新预置模板，通过 `TemplateId` 匹配并更新内容（用户修改过的标记为 `IsUserModified`，跳过覆盖）。

### 5. 多窗口并发

**场景**：理论上用户可能打开多个应用实例操作同一模板目录。

**处理**：当前为单实例桌面应用，暂不考虑多窗口并发。`RoleTemplateService` 内部使用 `SemaphoreSlim` 保护写操作，与 `ChatRoomPersistence` 的模式一致。

## 实施步骤

1. **新增 `RoleTemplate` 模型** — 创建 `RoleTemplate.cs` 和对应的 `JsonSerializerContext`。

2. **实现 `RoleTemplateService`** — 模板的加载、保存、删除、转换逻辑，以及预置模板初始化。

3. **定义预置模板** — 在 `PresetTemplates.cs` 中定义 6 个预置角色模板的完整定义。

4. **修改 `AppSettings`** — 新增 `RoleTemplatesPath` 可选配置项。

5. **实现 `RoleLobbyViewModel` 和 `RoleTemplateCardViewModel`** — 大厅的 ViewModel 逻辑，包括搜索、筛选、命令绑定。

6. **实现 `RoleLobbyView.axaml`** — 卡片网格布局，复用现有样式资源。

7. **修改 `MainViewModel` 导航** — 新增 `AppPage.RoleLobby` 枚举值和导航方法。

8. **修改 `MainView.axaml`** — 中栏新增 `RoleLobbyView` 面板。

9. **修改 `RoleListViewModel` 和 `SessionListViewModel`** — `RoleListViewModel` 新增"大厅"入口命令、"提升到角色大厅"命令及对应事件；`SessionListViewModel` 新增"角色大厅"入口命令和事件。

10. **修改对应 View 的 AXAML** — `RoleListView.axaml`：顶栏列定义改为三列、新增"大厅"按钮、角色项新增右键上下文菜单（含"提升到角色大厅"、"编辑角色"、"删除角色"三个菜单项）；`SessionListView.axaml`：底部栏改为 `StackPanel`、新增"角色大厅"按钮。

11. **修改 `App.axaml.cs`** — 初始化 `RoleTemplateService`，注入 `MainViewModel`。

12. **编写单元测试** — 覆盖：模板 CRUD、从模板创建角色定义、预置模板初始化、搜索筛选逻辑。

13. **编译验证 + 集成测试** — 确保与现有会话/角色管理功能不冲突。
