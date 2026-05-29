# Copilot 工作路径默认工具接入方式

## 场景

当 `CopilotViewModel.SendMessageAsync(...)` 没有显式传入工具时，希望仍然给模型一组默认文件系统工具，用来围绕当前已打开文件夹做只读查询。

同时需要保持现有边界：

- 默认工具定义留在 `AvaloniaAgentLib`，不依赖 `SimpleWrite` 的 View 或 ViewModel 类型；
- `SimpleWriteSideBar` 继续只负责左侧目录树 UI 与事件转发；
- 只有 `RightSlideBar` 作为右侧 Copilot 面板宿主，知道 `CopilotViewModel` 的存在，并负责把当前文件夹状态桥接过去。

## 当前实现

### 1. `CopilotViewModel` 暴露主副工作路径

`CopilotViewModel` 现在同时暴露：

- `WorkspacePath`：主工作路径；
- `SecondaryWorkspacePath`：副工作路径。

其中：

- 主工作路径优先代表“当前打开文件夹”的根目录；
- 副工作路径代表“当前文档所在文件夹”；
- 当主工作路径为空时，对外读取 `WorkspacePath` 仍会回落到副工作路径，保证默认文件工具还能解析当前文档附近的相对文件。

### 2. 默认工具通过 `CopilotToolManager` 管理

`CopilotViewModel` 现在拥有一个专门的 `CopilotToolManager`。

`WorkspaceToolProvider` 不再承担“全部 AI 工具入口”的角色，而是作为 `CopilotToolManager.WorkspaceTools` 的一个子能力存在。

这样做的原因是：

- 后续如果要增加非文件相关工具，不需要继续把它们硬塞进 `WorkspaceToolProvider`；
- `CopilotViewModel` 只和一个工具管理对象交互，扩展点更稳定；
- 工作路径状态可以直接保存在工具管理链路里，不需要通过委托回调去反向读取 ViewModel 属性。

当前默认工具包括：

1. 列出目录；
2. 读取文件开头内容；
3. 递归搜索匹配名称的文件或文件夹；
4. 递归搜索包含指定文本的文件，并返回文件路径与命中行号；
5. 按行范围读取文件内容；
6. `SubAgent`：把某个相对独立的任务委托给子代理执行。

对于相对路径，这些工具会区分主副工作区：

- 列目录、按名称递归搜索、按内容递归搜索，只允许使用主工作路径；
- 读取文件开头、按行读取文件，会先尝试主工作路径；若主路径为空，或主路径下不存在目标文件，则回退到副工作路径；
- 无论主副哪条路径生效，都仍然要求解析后的相对路径不能跳出各自根目录。

对于绝对路径，工具会按绝对路径直接解析；如果传入的是相对路径而当前又没有 `WorkspacePath`，则返回明确错误信息。

### 4. `SubAgent` 默认工具如何选择模型

`CopilotToolManager` 不再只聚合 `WorkspaceToolProvider`，而是同时持有：

- `WorkspaceToolProvider`：负责工作区只读文件工具；
- `SubAgentToolProvider`：负责子代理委托工具。

为了让 `SubAgentToolProvider` 能按能力选择模型，`CopilotToolManager` 构造时改为直接接收 `AgentApiEndpointManager`。

目前 `SubAgent` 工具暴露三个参数：

1. 给子代理的提示词；
2. 可空的系统提示词；
3. 用 `;` 分号分隔的子代理类型字符串。

支持的子代理类型有：

- `Flash`
- `ImageInput`
- `VideoInput`
- `ImageOutput`

其中 `Flash` 只适合确定性处理、总结输出、了解文件组织结构或大文件内容、意图识别等任务；凡是涉及分析、判断、设计或决策的内容，都不要选 `Flash`。

模型选择规则如下：

- 未写 `Flash` 时，只匹配非 Flash 模型；
- 写了 `ImageInput` / `VideoInput` / `ImageOutput` 时，要求模型声明对应输入或输出模态能力；
- 最终候选模型通过 `AgentApiEndpointManager.GetBestModel(...)` 结合现有能力比较器选出最佳模型；
- 若没有任何模型满足条件，则返回明确失败，而不是偷偷回退到不符合要求的模型。

子代理执行时只发送 system/user 消息，不继续挂默认工具，避免递归调用 `SubAgent` 自身。

### 3. `SendMessageAsync` 的工具选择策略

`CopilotViewModel.SendMessageAsync(...)` 现在会先走 `ResolveTools(...)`：

- 调用方显式提供工具且非空时，沿用调用方工具；
- 调用方未提供工具或工具为空时，自动附加 `WorkspaceToolProvider` 生成的默认工具。

这样不会影响已有“自定义工具优先”的调用方式，同时让普通聊天在打开文件夹后也具备基础查阅能力。

## 工作路径从哪里来

工作路径不由 `SimpleWriteSideBar` 直接设置给 `CopilotViewModel`。

实际桥接点在 `SimpleWrite/Views/Components/RightSlideBar.axaml.cs`：

- 右侧栏初始化 `CopilotViewModel` 时，同时订阅：
  - `FolderExplorerViewModel.CurrentFolderChanged`
  - `EditorViewModel.EditorModelChanged`
- 每次左侧已打开文件夹变化时，同步把 `CurrentFolder?.FullName` 写入 `copilotViewModel.WorkspacePath`；
- 每次当前文档切换时，同步把 `CurrentEditorModel.FileInfo?.DirectoryName` 写入 `copilotViewModel.SecondaryWorkspacePath`。

这样左侧栏仍然只知道 `FolderExplorerViewModel`，不会直接感知 `CopilotViewModel`。

`CopilotViewModel.WorkspacePath` / `SecondaryWorkspacePath` 本身都不再维护独立副本，而是直接代理到 `CopilotToolManager`，后者再同步到 `WorkspaceToolProvider`。

这种做法比“在 Provider 构造函数里传入 `Func<string?>` 回调”更稳妥，因为状态归属是明确的，读写路径也更容易追踪。

## 这次修正的经验

上一次实现之所以会走到委托注入状态这条路，本质上是把“减少耦合”误用了：

1. 看到 `WorkspaceToolProvider` 不应依赖 `SimpleWrite`，就进一步错误地把它设计成也不直接持有自己的必要状态；
2. 想让工具读取到 `CopilotViewModel.WorkspacePath` 的最新值，于是选了 `Func<string?>` 这条看似灵活、实际绕远的链路；
3. 当时又没有先抽象出上层工具管理类，导致文件工具类型被迫同时承担“具体工具实现”和“AI 工具入口聚合”两种职责。

后续应避免再犯同类问题：

- 不要用委托去转发本应由对象自身明确持有的状态；
- 先划清“工具管理”和“具体工具实现”的层级，再决定状态放在哪一层；
- 如果某个属性本质上服务于内部子对象，应优先让外层属性直接代理到真实状态拥有者，而不是复制一份或回调读取；
- 当默认工具需要额外基础设施能力时，优先在工具管理层注入依赖，不要把无关能力硬塞进某个具体工具提供器里。

## 额外注意

1. 默认文件工具是只读查询型工具，没有提供写文件或改目录结构的能力。
2. `SubAgent` 当前返回子代理聚合后的文本结果，若后续要支持多模态结果对象，需要先确认主调用链怎样承载结构化输出。
3. 工具返回内容都做了范围限制，例如最大结果数、最大字符数、最大行数，避免一次返回过大。
4. 如果当前没有打开文件夹，工具只会在需要解析相对路径时返回“未设置工作路径”的提示；若调用方给的是绝对路径，则仍可直接访问该路径。
5. 这次同时移除了 `SendMessageAsync` 里的调试死循环，否则会阻塞正常聊天流程。
6. 现在“工作路径”不再只等于左侧文件夹树：目录类工具只认主工作区，文件读取类工具则支持当前文档目录作为副工作区兜底。

## 适用场景

- 继续扩展 Copilot 的仓库内查阅能力时；
- 需要新增更多默认只读工具时；
- 排查“为什么打开文件夹后 Copilot 能直接搜索文件”这条链路时。
