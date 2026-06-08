# SessionTitleGenerator 委托注入问题分析与重构计划

## 背景

在实现会话标题自动生成功能时，`SessionTitleGenerator` 采用了 `Func<Task<IChatClient>>` 委托注入模式，将 `IChatClient` 的创建逻辑通过委托从 `CopilotChatManager` 传入。经代码审查发现，该设计存在多项不合理之处，需要进行重构。

## 问题分析

### 问题 1：每次调用都走完整解析链

当前 `CopilotChatManager` 注入的委托实现为 `CreateTitleChatClientAsync`：

```csharp
private async Task<IChatClient> CreateTitleChatClientAsync()
{
    var supportedModels = AgentApiEndpointManager.GetSupportedModels();

    var flashModel = supportedModels
        .Where(m => m.ModelDefinition.Capabilities?.IsFlash == true)
        .OrderByDescending(m => m, new LanguageModelCapabilityComparer())
        .FirstOrDefault();

    var model = flashModel ?? AgentApiEndpointManager.PrimaryModel;
    return await model.GetChatClientAsync().ConfigureAwait(false);
}
```

由于 `Func<Task<IChatClient>>` 是每次 `GenerateTitleAsync` 调用时执行的，每次标题生成都会：

- 调用 `GetSupportedModels()` 遍历全部已注册模型
- 执行 LINQ `Where` 过滤 + `OrderByDescending` 排序
- 新建 `LanguageModelCapabilityComparer` 实例
- 调用 `GetChatClientAsync()` 创建新的 `IChatClient`

在一次进程生命周期内，模型列表是注册时确定的，Flash 模型选择结果也是稳定的。这些操作每次都重跑一遍，纯粹浪费 CPU。

### 问题 2：委托没有实现真正的解耦

委托注入模式的初衷是"模型选择策略完全由调用方控制"。但实际情况是：

- `CreateTitleChatClientAsync` 是 `CopilotChatManager` 的 **`private`** 方法
- 它强依赖 `this.AgentApiEndpointManager`
- 不存在第二个调用方

所谓的"策略由调用方控制"是一句空话——调用方只有一个，策略也只有一种。委托只是把 `CopilotChatManager` 的内部逻辑绕了一圈又传回来，没有实现任何有意义的解耦。

如果未来确实需要不同的模型选择策略，正确的做法是让调用方传入不同的 `AgentApiEndpointManager` 实例，而不是传入一个委托。

### 问题 3：`IChatClient` 每次新建，浪费连接资源

`Microsoft.Extensions.AI` 的 `IChatClient`（尤其是基于 `HttpClient` 的实现）设计为可复用、可池化的长生命周期对象。

当前设计每次 `GenerateTitleAsync` 都调用 `model.GetChatClientAsync()` 创建新实例：

- 底层 `HttpClient` 连接无法复用
- 即使有 `IHttpClientFactory` 池化，也不如持有一个长生命周期实例
- 对 Flash 模型的轻量调用来说影响不大，但频繁创建/销毁仍有不必要的开销

### 问题 4：违反 KISS 原则

整个调用链是：

```
Func<Task<IChatClient>> → CreateTitleChatClientAsync() → AgentApiEndpointManager
```

绕了一圈回到原点。不如直接传 `AgentApiEndpointManager`，让 `SessionTitleGenerator` 自己管理模型解析和 `IChatClient` 生命周期。

### 问题 5：异步委托在构造函数中注册，时序不透明

```csharp
// CopilotChatManager 构造函数
_titleGenerator = new SessionTitleGenerator(CreateTitleChatClientAsync);
```

`CreateTitleChatClientAsync` 是一个 `private async Task<IChatClient>` 方法，它依赖 `this.AgentApiEndpointManager`。虽然构造时只是注册委托而不执行，但这种"延迟执行 + 捕获 `this`"的模式增加了心智负担——阅读代码时需要追踪委托最终在哪里被调用。

---

## 重构方案

### 核心思路

1. `SessionTitleGenerator` 直接依赖 `AgentApiEndpointManager`，移除委托
2. 模型解析结果（`ILanguageModel`）在构造时缓存
3. `IChatClient` 采用懒加载 + 缓存策略

### 新类结构

```
SessionTitleGenerator(AgentApiEndpointManager)
  ├── 字段: ILanguageModel? _titleModel      ← 构造时解析，不变
  ├── 字段: IChatClient? _chatClient         ← 首次调用时创建，后续复用
  │
  ├── GenerateTitleAsync(session, ct)
  │     ├── 守卫: TitleSource is Generated / UserSet → return
  │     ├── await EnsureChatClientAsync()    ← 懒加载
  │     └── chatClient.GetResponseAsync(...)
  │
  ├── ResolveTitleModel() → ILanguageModel?  ← Flash 优先, PrimaryModel 回退
  └── EnsureChatClientAsync() → IChatClient  ← 懒加载 + 缓存
```

### 模型选择策略（不变）

```
1. 从 AgentApiEndpointManager.GetSupportedModels() 中筛选 Flash 模型
   - 条件: m.ModelDefinition.Capabilities?.IsFlash == true
2. 如果找到 Flash 模型，按 LanguageModelCapabilityComparer 排序取最优
3. 如果找不到 Flash 模型，回退到 AgentApiEndpointManager.PrimaryModel
```

### 懒加载而非构造时加载的理由

- `GetChatClientAsync()` 是异步方法，不能放在构造函数中
- 如果会话永远不需要标题生成（如已通过 `SetTitle` 设为 `UserSet`），就不需要创建 `IChatClient`
- 遵循"需要时才创建"原则

### 生命周期对比

| | 重构前 | 重构后 |
|---|---|---|
| 模型解析 | 每次 `GenerateTitleAsync` 都执行 | 构造时执行一次，缓存 |
| `IChatClient` 创建 | 每次 `GenerateTitleAsync` 都新建 | 首次调用时创建，后续复用 |
| `LanguageModelCapabilityComparer` | 每次 `new` | 构造时 `new` 一次 |

---

## 需要变更的文件

### 1. `AgentLib\SessionTitleGenerator.cs`

- 构造函数签名：`Func<Task<IChatClient>>` → `AgentApiEndpointManager`
- 移除字段：`_chatClientFactory`
- 新增字段：`_endpointManager`、`_titleModel`、`_chatClient`
- 新增方法：`ResolveTitleModel()`、`EnsureChatClientAsync()`
- `GenerateTitleCoreAsync` 改为使用缓存的 `_chatClient`

### 2. `AgentLib\CopilotChatManager.cs`

- 构造函数中：`new SessionTitleGenerator(CreateTitleChatClientAsync)` → `new SessionTitleGenerator(AgentApiEndpointManager)`
- 移除私有方法：`CreateTitleChatClientAsync`

---

## 实施步骤

1. 修改 `SessionTitleGenerator` 构造函数签名，新增 `_endpointManager` 字段
2. 新增 `ResolveTitleModel()` 方法（Flash 优先 → PrimaryModel 回退）
3. 新增 `EnsureChatClientAsync()` 懒加载方法
4. 修改 `GenerateTitleCoreAsync` 使用缓存的 `_chatClient`
5. 更新 `CopilotChatManager` 构造函数调用
6. 移除 `CopilotChatManager.CreateTitleChatClientAsync`
7. 编译验证
