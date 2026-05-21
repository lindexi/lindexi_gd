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
        Tools = [];
    }

    public string ServerUrl { get; }

    public string StatusText { get; }

    public string ResultText { get; }

    public ObservableCollection<McpToolViewModel> Tools { get; }
}
