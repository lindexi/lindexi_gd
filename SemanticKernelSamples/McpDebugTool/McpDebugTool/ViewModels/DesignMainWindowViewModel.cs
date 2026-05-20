using System.Collections.ObjectModel;
using System.Text.Json;

namespace McpDebugTool.ViewModels;

public sealed class DesignMainWindowViewModel : ObservableObject
{
    public DesignMainWindowViewModel()
    {
        ServerUrl = "https://localhost:5001/mcp";
        StatusText = "已连接到本地调试服务";
        ResultText = "{\n  \"content\": [\n    {\n      \"type\": \"text\",\n      \"text\": \"Hello MCP\"\n    }\n  ],\n  \"isError\": false\n}";
        Tools =
        [
            new McpToolViewModel(
                "echo",
                "返回输入的文本内容。",
                JsonDocument.Parse("""
                {
                  "type": "object",
                  "properties": {
                    "message": {
                      "type": "string",
                      "description": "要发送的消息"
                    },
                    "repeat": {
                      "type": "integer",
                      "description": "重复次数"
                    },
                    "enabled": {
                      "type": "boolean",
                      "description": "是否启用"
                    }
                  },
                  "required": ["message"]
                }
                """).RootElement.Clone())
            {
                IsExpanded = true,
            },
            new McpToolViewModel(
                "sum",
                "计算输入数字的总和。",
                JsonDocument.Parse("""
                {
                  "type": "object",
                  "properties": {
                    "numbers": {
                      "type": "array",
                      "description": "请输入 JSON 数组，例如 [1,2,3]"
                    }
                  },
                  "required": ["numbers"]
                }
                """).RootElement.Clone())
        ];
    }

    public string ServerUrl { get; }

    public string StatusText { get; }

    public string ResultText { get; }

    public ObservableCollection<McpToolViewModel> Tools { get; }
}
