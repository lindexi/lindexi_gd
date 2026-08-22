# DeepSeek Workspace

DeepSeek Workspace 是面向个人开发者的 Windows 桌面 AI 工作区，支持 AgentLib 已注册模型、真实多轮流式 reasoning/text、图片输入、Agent 工作区工具、本地会话管理、Markdown/代码展示、导出和本地诊断。

## 系统要求

- Windows 10 或 Windows 11 x64
- .NET 10 Desktop Runtime
- 可访问所配置模型服务的网络
- 有效的模型端点和 Key

发布包是 framework-dependent zip，不包含 .NET Runtime。

## 快速开始

1. 安装 .NET 10 Desktop Runtime。
2. 解压发布 zip 到可写目录。
3. 创建或编辑 Agent 配置文件：
	  - 默认：`%LocalAppData%\DeepSeekWpf\AgentConfiguration.json`
   - 自定义：将环境变量 `DEEPSEEKWPF_AGENT_CONFIG` 设置为配置文件完整路径。
4. 使用合法 JSON 填写端点、Key 和模型定义。
5. 启动 `DeepSeekWpf.exe`。
6. 打开“设置”，点击“重新加载配置”，选择模型并测试连接。
7. 返回对话工作区开始使用。
8. 如需发送图片，可在输入框下方点击“附加图片”；当前支持选择 PNG、JPEG、WebP 和 GIF。

如果配置文件不存在，应用会创建一个合法的空配置；需要先补全端点和模型后才能发送消息。

## Agent 配置示例

以下示例是可解析的标准 JSON，不包含注释。请替换占位 Key，并按实际服务调整模型名、能力和限制。

```json
{
  "PrimaryModel": "deepseek/deepseek-chat",
  "OpenAIConfigurationList": [
	{
	  "EndPoint": "https://api.deepseek.com",
	  "Key": "YOUR_DEEPSEEK_API_KEY",
	  "ModelDefinitions": [
		{
		  "Provider": "deepseek",
		  "ModelName": "deepseek-chat",
		  "ModelId": "deepseek-chat",
		  "Capabilities": {
			"Temperature": true,
			"Reasoning": false,
			"Attachment": false,
			"ToolCall": true,
			"Input": {
			  "Text": true,
			  "Image": false,
			  "Audio": false,
			  "Video": false,
			  "Pdf": false
			},
			"Output": {
			  "Text": true,
			  "Image": false,
			  "Audio": false,
			  "Video": false,
			  "Pdf": false
			},
			"Interleaved": false,
			"IsFlash": false,
			"ResponseFormat": false
		  },
		  "ContextWindowSize": 64000,
		  "MaxOutputTokens": 8192
		}
	  ]
	}
  ]
}
```

`PrimaryModel` 可使用应用显示的 `Provider/ModelName` 形式。DeepSeek Workspace 会加载 AgentLib 当前注册的全部提供者；设置页只选择模型，不读取、编辑或回显 Key。应用不会根据模型能力元数据禁用图片入口；若所选模型或服务不接受图片，请求失败信息会按真实服务响应展示。

## 构建、测试与发布

在仓库根目录执行：

```powershell
dotnet restore SemanticKernelSamples/DeepSeekWpf.Tests/DeepSeekWpf.Tests.csproj
dotnet build SemanticKernelSamples/DeepSeekWpf/DeepSeekWpf.csproj -c Release
dotnet test SemanticKernelSamples/DeepSeekWpf.Tests/DeepSeekWpf.Tests.csproj -c Release
powershell -ExecutionPolicy Bypass -File SemanticKernelSamples/DeepSeekWpf/build/publish.ps1 -Version 1.0.0
```

默认发布目录为 `SemanticKernelSamples/DeepSeekWpf/artifacts/publish/win-x64`。项目目标框架为 .NET 10；`global.json` 不固定到某个本机 SDK 补丁版本。

## 数据位置

默认位置：

- Agent 配置：`%LocalAppData%\DeepSeekWpf\AgentConfiguration.json`
- 应用设置：`%LocalAppData%\DeepSeekWpf\settings.json`
- 会话：`%LocalAppData%\DeepSeekWpf\Data\Sessions\*.json`
- 缓存：`%LocalAppData%\DeepSeekWpf\Cache`
- 日志：`%LocalAppData%\DeepSeekWpf\Logs\app-YYYYMMDD.log`

缓存、数据和日志目录可在设置页修改。修改前建议备份原目录；应用不会自动搬迁旧目录中的文件。

## 隐私

- Key 仅由外部 Agent 配置文件管理。
- 应用设置、会话、日志和诊断摘要不主动保存 Key 或认证头。
- 日志默认不记录聊天正文。
- 发送的图片会复制并编码到本地会话 JSON 中，原文件移动或删除后历史消息仍可恢复；图片也会随请求发送给所选模型服务。
- Agent 工作区工具以当前数据目录为工作区，模型可能根据请求读取或修改该目录内允许访问的内容；请勿把不希望模型处理的文件放入该目录。
- 应用不自动上传日志、诊断摘要、会话或其他用户数据。
- 分享诊断资料时不要包含 Key、Agent 配置文件或聊天正文，并检查路径中是否有敏感用户名。

## 快捷键

- `Ctrl+N`：新建会话
- `Ctrl+,`：打开设置
- `Ctrl+F`：聚焦会话搜索
- 默认 `Enter`：发送消息
- `Shift+Enter`：换行
- 关闭“按 Enter 发送”后，使用 `Ctrl+Enter` 发送

更多信息见 [Docs/README.md](Docs/README.md)、[Docs/支持与排障.md](Docs/支持与排障.md) 和 [Docs/发布说明/1.0.0.md](Docs/发布说明/1.0.0.md)。
