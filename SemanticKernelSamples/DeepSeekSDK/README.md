# DeepSeekSDK

.NET 10 的 DeepSeek 网页搜索工具客户端，调用 `POST https://api.deepseek.com/v1/tools/search`。

## 使用方式

创建并复用 `HttpClient`，将其与 API Key 传给 `DeepSeekClient`。调用 `SearchAsync`，传入 `DeepSeekSearchRequest` 及可选的 `CancellationToken`，通过返回值的 `Results` 获取搜索结果。

以下为当前服务端文档中的参数约定；客户端不做参数防御校验，直接序列化发送，由服务端决定是否接受，避免固化可能变化的约束。

- `Query` 必填，长度为 1～128 个 Unicode 字符，不能全为空白。
- `Count` 默认为 10，允许 1～50。
- `OriginalQuestion` 默认为 null；提供时同样要求 1～128 个 Unicode 字符且不能全为空白。
- `Localization` 默认为空字符串。
- `NeedContent` 默认为 false。为 true 时请求网页正文，但正文缺失仍可能返回空字符串。

客户端不持有或释放 `HttpClient`，应由调用方管理其生命周期及超时。API Key 按请求设置，不修改共享 HTTP 客户端的默认请求头。

HTTP 非成功状态抛出 `HttpRequestException`，可从 `StatusCode` 读取状态码；取消抛出 `OperationCanceledException`；非法 JSON 或缺失必需响应字段抛出 `System.Text.Json.JsonException`。客户端不自动重试，避免对可能计费的请求产生重复调用。

## 测试

`DeepSeekSDK.slnx` 包含 MSTest 测试项目 `Code/DeepSeekSDK.Tests`：

- 单元测试使用内存 HTTP 处理器，验证协议、默认值、响应映射、参数原样发送、HTTP 错误及取消，不访问网络。
- `Integration` 分类包含两次真实搜索，分别验证 `need_content=false` 和 `true`，每次最多返回两条结果，可能产生费用。
- 真实测试仅从代码中固定的 `c:\lindexi\Work\Key\deepseek.txt` 读取 API Key，并移除文件首尾空白。文件不存在时标记为跳过（Inconclusive）；存在但为空、不可读或接口失败时正常报告失败，不隐藏配置或服务错误。
- 不使用环境变量或其他密钥来源，也不输出 API Key。请勿将密钥文件加入版本控制。

运行整个解决方案测试会包含真实调用；仅运行无费用的单元测试时，使用测试筛选表达式 `TestCategory!=Integration`。
