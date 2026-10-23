# DeepSeekSDK

单目标 .NET 6（`net6.0`）的 DeepSeek 工具客户端，沿用仓库的 `LangVersion=latest`，通过 `DotNetCampus.LatestCSharpFeatures` 13.0.1 支持 `required` 等现代 C# 语法，支持网页搜索 `POST https://api.deepseek.com/v1/tools/search` 和网页抓取 `POST https://api.deepseek.com/v1/tools/fetch`。

通过支持 .NET 6 的 `System.Text.Json` 8.0.6 包提供 `JsonRequired` 支持：DTO 原有必需 JSON 字段仍必须出现，缺失时反序列化抛出 `JsonException`，包括允许值为 null 的必需字段。必需属性同时使用 `required` 提供对象初始化时的编译期检查，并保留 `JsonRequired`，确保 JSON 缺失字段校验不变。

## 使用方式

创建并复用 `HttpClient`，将其与 API Key 传给 `DeepSeekClient`。调用 `SearchAsync`，传入 `DeepSeekSearchRequest` 及可选的 `CancellationToken`，通过返回值的 `Results` 获取搜索结果。

以下为当前服务端文档中的参数约定；客户端不做参数防御校验，直接序列化发送，由服务端决定是否接受，避免固化可能变化的约束。

- `Query` 必填，长度为 1～128 个 Unicode 字符，不能全为空白。
- `Count` 默认为 10，允许 1～50。
- `OriginalQuestion` 默认为 null；提供时同样要求 1～128 个 Unicode 字符且不能全为空白。
- `Localization` 默认为空字符串。
- `NeedContent` 默认为 false。为 true 时请求网页正文，但正文缺失仍可能返回空字符串。

### 网页抓取

调用 `FetchAsync`，传入 `DeepSeekFetchRequest` 及可选的 `CancellationToken`。`Url` 原样发送给服务端；当前服务端要求长度不超过 8192，必须为含 host 的 HTTP/HTTPS URL，且不能携带用户名或密码，客户端不做这些校验。

返回的 `DeepSeekFetchResponse.Title` 为页面标题（页面标题缺失时为空字符串），`Content` 为抓取后的 Markdown 正文。

客户端不持有或释放 `HttpClient`，应由调用方管理其生命周期及超时。API Key 按请求设置，不修改共享 HTTP 客户端的默认请求头。

HTTP 非成功状态会尝试读取一次响应正文，并直接抛出包含完整上下文的 `HttpRequestException`，不再由外层捕获包装。其 `Message` 包含失败操作（Search/Fetch）、API endpoint、业务目标（Fetch 的 Url，Search 的 Query、Count、NeedContent、Localization 和 OriginalQuestion）、HTTP 状态码及原样保留的服务端正文（包括正文中的 request_id）。正文为空白或读取失败时，优先使用响应的 `ReasonPhrase`，为 null 时使用 `StatusCode` 枚举名，不再查找 `Status{int}` 资源说明。正文读取失败的异常对象直接作为 `InnerException` 保留；读取成功时 `InnerException` 为 null，`StatusCode` 始终保留非成功响应状态码。`SendAsync` 及成功响应正文读取抛出的 `HttpRequestException` 分别在窄范围捕获中补充操作、endpoint、业务上下文及原异常的 HTTP 状态码，原异常直接作为 `InnerException` 保留，状态码保持不变，不捕获或重复包装上述 SDK 非成功状态异常。

上下文由调用处明确列出，不为诊断信息泛型序列化未知对象，也不附加请求头、Authorization 或客户端 API Key；服务端错误正文和原始异常消息原样保留，不做密钥替换或补丁脱敏。服务端或第三方 handler 回显的敏感信息不会被改写，应由其负责避免。业务目标和服务端正文可能包含业务敏感数据，调用方应控制日志访问权限。

调用方主动取消继续抛出 `OperationCanceledException`；非法 JSON 或缺失必需响应字段抛出 `System.Text.Json.JsonException`；超时由 HttpClient 通过取消异常报告。客户端不自动重试，避免对可能计费的请求产生重复调用。

`AgentLib.Coding` 的 Search/Fetch 工具边界将预期的 `HttpRequestException` 转为 AI 可读字符串（保留 SDK 的失败操作、目标和错误信息），并将无效响应 JSON、非调用方主动取消的超时/传输取消转为带操作、endpoint 及业务目标的中文提示。调用方主动取消继续传播；程序错误不转换为工具结果。

## 测试

`DeepSeekSDK.slnx` 包含 MSTest 测试项目 `Code/DeepSeekSDK.Tests`（测试项目保持 `net10.0`，引用单目标 `net6.0` 的 SDK）：

- 所有测试均属于 `Integration` 分类，直接调用真实后台，不使用 StubHandler 或伪造响应。
- 仅保留 `DeepSeekClientTests` 一个测试类，通过参数化测试覆盖三次真实搜索：一次验证默认参数（最多返回十条结果），另两次分别验证 `need_content=false` 和 `true`（每次最多返回两条结果），共用密钥读取、HttpClient 创建和结果检查。另外通过一次真实 Fetch 调用抓取 `https://api-docs.deepseek.com/`，验证标题及正文。上述调用可能产生费用。
- 真实测试仅从代码中固定的 `c:\lindexi\Work\Key\deepseek.txt` 读取 API Key，并移除文件首尾空白。文件不存在时直接静默 return，不发送请求，测试框架将其计为通过；存在但为空、不可读或接口失败时正常报告失败，不隐藏配置或服务错误。
- 不使用环境变量或其他密钥来源，也不输出 API Key。请勿将密钥文件加入版本控制。

运行整个解决方案测试会调用真实后台；密钥文件存在时会产生实际请求，不应将其视为无费用的离线测试。

离线回归测试位于已有的 [`../AgentLib/AgentLib.Coding.Tests/DeepSeekWebToolSourceTests.cs`](../AgentLib/AgentLib.Coding.Tests/DeepSeekWebToolSourceTests.cs)，使用 fake HttpMessageHandler，覆盖工具失败返回值、SDK HTTP/传输异常及成功响应正文读取失败的上下文和状态码/异常链、JSON 错误、超时、主动取消及服务错误内容原样保留。仅筛选该测试类运行不会访问真实 DeepSeek 服务或产生费用。
