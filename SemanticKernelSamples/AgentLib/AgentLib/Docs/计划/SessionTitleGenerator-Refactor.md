# SessionTitleGenerator 重构：移除委托注入

## 现状分析

```
CopilotChatManager ──(构造时注入 Func<Task<IChatClient>>)──▶ SessionTitleGenerator
                                                                │
                          每次 GenerateTitleAsync 调用时 ──▶ _chatClientFactory()
                                                                │
                                                ──▶ CreateTitleChatClientAsync()
                                                      │
                                                      ├── GetSupportedModels()
                                                      ├── LINQ Where + OrderByDescending
                                                      ├── new LanguageModelCapabilityComparer()
                                                      └── GetChatClientAsync()
```

## 委托注入的问题

### 问题 1：每次调用都走完整解析链

`Func<Task<IChatClient>>` 每次 `GenerateTitleAsync` 被调用时都执行，意味着：

- `AgentApiEndpointManager.GetSupportedModels()` → 遍历全部已注册模型 → 每次都跑
- `.Where(m => m.ModelDefinition.Capabilities?.IsFlash == true)` → 重新过滤
- `OrderByDescending(... new LanguageModelCapabilityComparer())` → 重新排序
- `model.GetChatClientAsync()` → 每次都新建 `IChatClient` 实例

这些结果在一次进程生命周期内几乎不变：模型列表是注册时确定的，Flash 选择结果也是稳定的。

### 问题 2：委托没有起到解耦作用

`CreateTitleChatClientAsync` 是 `CopilotChatManager` 的 **`private`** 方法：

```csharp
private async Task<IChatClient> CreateTitleChatClientAsync()
{
    var supportedModels = AgentApiEndpointManager.GetSupportedModels();
    // ...
    var model = flashModel ?? AgentApiEndpointManager.PrimaryModel;
    return await model.GetChatClientAsync().ConfigureAwait(false);
}
```

这个委托**永远只能由 `CopilotChatManager` 自身提供**，不存在第二个调用方。换言之：

- 所谓的"模型选择策略完全由调用方控制"是一句空话——调用方只有一个
- 委托只是把 `CopilotChatManager` 的内部逻辑绕了一圈又传回来
- 如果未来有其他调用方需要不同策略，直接传不同的 `AgentApiEndpointManager` 实例即可

### 问题 3：`IChatClient` 每次新建，浪费资源

`Microsoft.Extensions.AI` 的 `IChatClient`（尤其是基于 `HttpClient` 的实现）通常设计为可复用、可池化的长生命周期对象。

当前设计每次 `GenerateTitleAsync` 创建一个新的 `IChatClient`：

- 底层 `HttpClient` 连接不能复用（即使有 `IHttpClientFactory` 池化，也不如持有一个长生命周期实例）
- 对 Flash 模型来说这只是轻量调用，但频繁创建/销毁仍有开销

### 问题 4：违反 KISS

绕一圈回到原地：`Func<Task<IChatClient>>` → `CreateTitleChatClientAsync` → `AgentApiEndpointManager`。

不如直接传 `AgentApiEndpointManager`，让 `SessionTitleGenerator` 自己管理模型解析和 `IChatClient` 生命周期。

---

## 重构设计

### 目标

1. `SessionTitleGenerator` 直接依赖 `AgentApiEndpointManager`，移除委托
2. 模型解析结果缓存（`ILanguageModel` 不会变）
3. `IChatClient` 懒加载 + 缓存（首次调用时创建，后续复用）

### 新类结构

```
SessionTitleGenerator(AgentApiEndpointManager)
  ├── 字段: ILanguageModel? _titleModel      ← 构造时解析
  ├── 字段: IChatClient? _chatClient         ← 首次调用时创建，后续复用
  │
  ├── GenerateTitleAsync(session, ct)
  │     ├── 守卫: TitleSource is Generated / UserSet → return
  │     ├── await EnsureChatClientAsync()    ← 懒加载
  │     └── chatClient.GetResponseAsync(...)
  │
  └── ResolveTitleModel() → ILanguageModel?  ← Flash 优先, PrimaryModel 回退
```

### 生命周期

```
构造 SessionTitleGenerator(manager)
  └── ResolveTitleModel() → 缓存 _titleModel
         │
首次 GenerateTitleAsync()
  └── _titleModel.GetChatClientAsync() → 缓存 _chatClient
         │
后续 GenerateTitleAsync()
  └── 复用 _chatClient
```

### 懒加载而非构造时加载的理由

- `GetChatClientAsync()` 是 async，不能放在构造函数中
- 如果会话永远不需要标题生成（如已通过 `SetTitle` 设为 `UserSet`），就不需要创建 `IChatClient`
- 遵循"需要时才创建"原则

---

## 需要变更的文件

| 文件 | 变更 |
|---|---|
| `AgentLib\SessionTitleGenerator.cs` | 构造函数改为接收 `AgentApiEndpointManager`，移除 `Func<Task<IChatClient>>` 字段，新增 `_titleModel`、`_chatClient` 缓存 |
| `AgentLib\CopilotChatManager.cs` | 构造 `SessionTitleGenerator` 时传入 `AgentApiEndpointManager`，移除 `CreateTitleChatClientAsync` 私有方法 |

---

## 实施步骤

1. 修改 `SessionTitleGenerator` 构造函数：`Func<Task<IChatClient>>` → `AgentApiEndpointManager`
2. `SessionTitleGenerator` 内新增 `ResolveTitleModel()` 和 `EnsureChatClientAsync()` 辅助方法
3. `GenerateTitleCoreAsync` 改为使用 `_chatClient` 缓存
4. `CopilotChatManager`：更新构造函数调用，移除 `CreateTitleChatClientAsync`
5. 编译验证
