using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using McpDebugTool.Services;
using ModelContextProtocol.Protocol;

namespace McpDebugTool.ViewModels;

public class MainWindowViewModel : ObservableObject, IAsyncDisposable
{
    private readonly McpClientService? _mcpClientService;
    private CancellationTokenSource? _requestCancellationTokenSource;
    private string _serverUrl = string.Empty;
    private string _statusText = "请输入 MCP HTTP 地址并连接。";
    private string _resultText = "连接后可在此查看工具返回内容。";
    private bool _isBusy;
    private McpToolViewModel? _selectedTool;

    public MainWindowViewModel()
    {
        Tools = [];
        ConnectCommand = new AsyncRelayCommand(ConnectAsync, CanConnect);
        CallToolCommand = new AsyncRelayCommand(CallToolAsync, CanCallTool);
        LoadDesignData();
    }

    internal MainWindowViewModel(McpClientService mcpClientService)
    {
        ArgumentNullException.ThrowIfNull(mcpClientService);

        _mcpClientService = mcpClientService;
        Tools = [];
        ConnectCommand = new AsyncRelayCommand(ConnectAsync, CanConnect);
        CallToolCommand = new AsyncRelayCommand(CallToolAsync, CanCallTool);
        ServerUrl = "https://localhost:5001/mcp";
    }

    public ObservableCollection<McpToolViewModel> Tools { get; }

    public AsyncRelayCommand ConnectCommand { get; }

    public AsyncRelayCommand CallToolCommand { get; }

    public string ServerUrl
    {
        get => _serverUrl;
        set
        {
            if (SetProperty(ref _serverUrl, value))
            {
                ConnectCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string ResultText
    {
        get => _resultText;
        private set => SetProperty(ref _resultText, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                ConnectCommand.NotifyCanExecuteChanged();
                CallToolCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool HasTools => Tools.Count > 0;

    public bool HasNoTools => !HasTools;

    public McpToolViewModel? SelectedTool
    {
        get => _selectedTool;
        set
        {
            if (SetProperty(ref _selectedTool, value))
            {
                foreach (McpToolViewModel tool in Tools)
                {
                    tool.IsExpanded = ReferenceEquals(tool, value);
                }

                CallToolCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _requestCancellationTokenSource?.Cancel();
        _requestCancellationTokenSource?.Dispose();
        _requestCancellationTokenSource = null;

        if (_mcpClientService is not null)
        {
            await _mcpClientService.DisposeAsync();
        }
    }

    protected void LoadDesignData()
    {
        if (_mcpClientService is not null)
        {
            return;
        }

        _serverUrl = "https://localhost:5001/mcp";
        _statusText = "已连接到本地调试服务";
        _resultText = "{\n  \"content\": [\n    {\n      \"type\": \"text\",\n      \"text\": \"Hello MCP\"\n    }\n  ],\n  \"isError\": false\n}";

        Tools.Add(new McpToolViewModel(
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
        });

        Tools.Add(new McpToolViewModel(
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
            """).RootElement.Clone()));

        SelectedTool = Tools.FirstOrDefault();
    }

    private bool CanConnect()
    {
        return !IsBusy && !string.IsNullOrWhiteSpace(ServerUrl);
    }

    private bool CanCallTool()
    {
        return !IsBusy && SelectedTool?.Tool is not null;
    }

    private bool CanCallTool(object? parameter)
    {
        if (IsBusy)
        {
            return false;
        }

        return ResolveTool(parameter)?.Tool is not null;
    }

    private async Task ConnectAsync()
    {
        if (_mcpClientService is null)
        {
            return;
        }

        IsBusy = true;
        StatusText = "正在连接 MCP 服务...";
        ResultText = "";

        CancellationToken cancellationToken = ResetCancellation().Token;

        try
        {
            var tools = await _mcpClientService.ConnectAsync(ServerUrl, cancellationToken);

            Tools.Clear();
            foreach (var tool in tools)
            {
                Tools.Add(new McpToolViewModel(tool));
            }

            SelectedTool = Tools.FirstOrDefault();
            OnPropertyChanged(nameof(HasTools));
            OnPropertyChanged(nameof(HasNoTools));
            StatusText = string.Format(CultureInfo.CurrentCulture, "连接成功，共发现 {0} 个工具。", Tools.Count);
            ResultText = Tools.Count == 0 ? "当前 MCP 服务未声明任何工具。" : "请选择工具并填写参数后调用。";
        }
        catch (OperationCanceledException)
        {
            StatusText = "连接已取消。";
        }
        catch (Exception ex)
        {
            Tools.Clear();
            SelectedTool = null;
            OnPropertyChanged(nameof(HasTools));
            OnPropertyChanged(nameof(HasNoTools));
            StatusText = "连接失败";
            ResultText = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CallToolAsync(object? parameter)
    {
        McpToolViewModel? toolViewModel = ResolveTool(parameter);
        if (_mcpClientService is null || toolViewModel?.Tool is null)
        {
            return;
        }

        SelectedTool = toolViewModel;

        IsBusy = true;
        StatusText = string.Format(CultureInfo.CurrentCulture, "正在调用工具 {0} ...", toolViewModel.Name);

        CancellationToken cancellationToken = ResetCancellation().Token;

        try
        {
            var arguments = toolViewModel.BuildArguments();
            CallToolResult result = await _mcpClientService.CallToolAsync(toolViewModel.Tool, arguments, cancellationToken);

            StatusText = result.IsError is true
                ? string.Format(CultureInfo.CurrentCulture, "工具 {0} 返回错误。", toolViewModel.Name)
                : string.Format(CultureInfo.CurrentCulture, "工具 {0} 调用完成。", toolViewModel.Name);
            ResultText = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (OperationCanceledException)
        {
            StatusText = "调用已取消。";
        }
        catch (Exception ex)
        {
            StatusText = string.Format(CultureInfo.CurrentCulture, "工具 {0} 调用失败。", toolViewModel.Name);
            ResultText = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private McpToolViewModel? ResolveTool(object? parameter)
    {
        return parameter as McpToolViewModel ?? SelectedTool;
    }

    private CancellationTokenSource ResetCancellation()
    {
        _requestCancellationTokenSource?.Cancel();
        _requestCancellationTokenSource?.Dispose();
        _requestCancellationTokenSource = new CancellationTokenSource();
        return _requestCancellationTokenSource;
    }
}
