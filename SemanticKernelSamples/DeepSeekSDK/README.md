# DeepSeekSDK

.NET 10 的 DeepSeek 工具客户端，支持网页搜索 `POST https://api.deepseek.com/v1/tools/search` 和网页抓取 `POST https://api.deepseek.com/v1/tools/fetch`。

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

HTTP 非成功状态会在 try-catch 中尝试读取一次响应正文，优先作为 `HttpRequestException.Message`；正文为空白或读取失败时，使用 400、401、402、403、404、408、413、429、500、504 对应的说明，其他状态使用 HTTP 原因短语。读取失败的异常保留在 `InnerException`，调用方主动取消不会被吞掉。可从 `StatusCode` 读取原始状态码；取消抛出 `OperationCanceledException`；非法 JSON 或缺失必需响应字段抛出 `System.Text.Json.JsonException`。客户端不自动重试，避免对可能计费的请求产生重复调用。

## 测试

`DeepSeekSDK.slnx` 包含 MSTest 测试项目 `Code/DeepSeekSDK.Tests`：

- 所有测试均属于 `Integration` 分类，直接调用真实后台，不使用 StubHandler 或伪造响应。
- 仅保留 `DeepSeekClientTests` 一个测试类，通过参数化测试覆盖三次真实搜索：一次验证默认参数（最多返回十条结果），另两次分别验证 `need_content=false` 和 `true`（每次最多返回两条结果），共用密钥读取、HttpClient 创建和结果检查。另外通过一次真实 Fetch 调用抓取 `https://api-docs.deepseek.com/`，验证标题及正文。上述调用可能产生费用。
- 真实测试仅从代码中固定的 `c:\lindexi\Work\Key\deepseek.txt` 读取 API Key，并移除文件首尾空白。文件不存在时直接静默 return，不发送请求，测试框架将其计为通过；存在但为空、不可读或接口失败时正常报告失败，不隐藏配置或服务错误。
- 不使用环境变量或其他密钥来源，也不输出 API Key。请勿将密钥文件加入版本控制。

运行整个解决方案测试会调用真实后台；密钥文件存在时会产生实际请求，不应将其视为无费用的离线测试。
