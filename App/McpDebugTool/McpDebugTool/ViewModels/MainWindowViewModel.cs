using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
    private readonly List<McpToolViewModel> _allTools = [];
    private CancellationTokenSource? _connectCancellationTokenSource;
    private string _serverUrl = string.Empty;
    private string _searchText = string.Empty;
    private string _statusText = "请输入 MCP HTTP 地址并连接。";
    private bool _isConnecting;
    private McpToolViewModel? _selectedTool;

    public MainWindowViewModel()
    {
        Tools = [];
        ConnectCommand = new AsyncRelayCommand(ConnectAsync, CanConnect);
        CallToolCommand = new AsyncRelayCommand(CallToolAsync, CanCallTool, allowConcurrentExecutions: true);
        StopToolCommand = new AsyncRelayCommand(StopToolAsync, CanStopTool, allowConcurrentExecutions: true);
        LoadDesignData();
    }

    internal MainWindowViewModel(McpClientService mcpClientService)
    {
        ArgumentNullException.ThrowIfNull(mcpClientService);

        _mcpClientService = mcpClientService;
        Tools = [];
        ConnectCommand = new AsyncRelayCommand(ConnectAsync, CanConnect);
        CallToolCommand = new AsyncRelayCommand(CallToolAsync, CanCallTool, allowConcurrentExecutions: true);
        StopToolCommand = new AsyncRelayCommand(StopToolAsync, CanStopTool, allowConcurrentExecutions: true);
        ServerUrl = "https://localhost:5001/mcp";
    }

    public ObservableCollection<McpToolViewModel> Tools { get; }

    public AsyncRelayCommand ConnectCommand { get; }

    public AsyncRelayCommand CallToolCommand { get; }

    public AsyncRelayCommand StopToolCommand { get; }

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

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyToolFilter();
            }
        }
    }

    public bool IsConnecting
    {
        get => _isConnecting;
        private set
        {
            if (SetProperty(ref _isConnecting, value))
            {
                ConnectCommand.NotifyCanExecuteChanged();
                CallToolCommand.NotifyCanExecuteChanged();
                StopToolCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool HasTools => Tools.Count > 0;

    public bool HasNoTools => !HasTools;

    public bool HasSelectedTool => SelectedTool is not null;

    public bool HasNoSelectedTool => !HasSelectedTool;

    public McpToolViewModel? SelectedTool
    {
        get => _selectedTool;
        set
        {
            if (SetProperty(ref _selectedTool, value))
            {
                OnPropertyChanged(nameof(HasSelectedTool));
                OnPropertyChanged(nameof(HasNoSelectedTool));
                CallToolCommand.NotifyCanExecuteChanged();
                StopToolCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _connectCancellationTokenSource?.Cancel();
        _connectCancellationTokenSource?.Dispose();
        _connectCancellationTokenSource = null;

        foreach (McpToolViewModel tool in _allTools)
        {
            tool.CancelInvocation();
        }

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
        SearchText = string.Empty;

        McpToolViewModel echoTool = new(
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
        ;
        echoTool.CompleteInvocation("最近一次调用成功。", "{\n  \"content\": [\n    {\n      \"type\": \"text\",\n      \"text\": \"Hello MCP\"\n    }\n  ],\n  \"isError\": false\n}");

        RegisterTool(echoTool);

        RegisterTool(new McpToolViewModel(
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

        ApplyToolFilter();
    }

    private bool CanConnect()
    {
        return !IsConnecting && !string.IsNullOrWhiteSpace(ServerUrl) && _allTools.All(tool => !tool.IsCalling);
    }

    private bool CanCallTool(object? parameter)
    {
        if (IsConnecting)
        {
            return false;
        }

        McpToolViewModel? tool = ResolveTool(parameter);
        return tool?.Tool is not null && !tool.IsCalling;
    }

    private bool CanStopTool(object? parameter)
    {
        if (IsConnecting)
        {
            return false;
        }

        return ResolveTool(parameter)?.IsCalling is true;
    }

    private async Task ConnectAsync()
    {
        if (_mcpClientService is null)
        {
            return;
        }

        IsConnecting = true;
        StatusText = "正在连接 MCP 服务...";
        CancellationToken cancellationToken = ResetConnectCancellation().Token;

        try
        {
            var tools = await _mcpClientService.ConnectAsync(ServerUrl, cancellationToken);

            _allTools.Clear();
            foreach (var tool in tools)
            {
                RegisterTool(new McpToolViewModel(tool));
            }

            ApplyToolFilter();
            StatusText = string.Format(CultureInfo.CurrentCulture, "连接成功，共发现 {0} 个工具。", _allTools.Count);
        }
        catch (OperationCanceledException)
        {
            StatusText = "连接已取消。";
        }
        catch (Exception ex)
        {
            McpToolViewModel? selectedTool = SelectedTool;
            _allTools.Clear();
            Tools.Clear();
            SelectedTool = null;
            OnPropertyChanged(nameof(HasTools));
            OnPropertyChanged(nameof(HasNoTools));
            StatusText = "连接失败";
            if (selectedTool is not null)
            {
                selectedTool.CompleteInvocation("连接失败。", ex.Message);
            }
        }
        finally
        {
            IsConnecting = false;
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
        StatusText = string.Format(CultureInfo.CurrentCulture, "正在调用工具 {0} ...", toolViewModel.Name);
        CancellationToken cancellationToken = toolViewModel.StartInvocation();
        ConnectCommand.NotifyCanExecuteChanged();

        try
        {
            var arguments = toolViewModel.BuildArguments();
            CallToolResult result = await _mcpClientService.CallToolAsync(toolViewModel.Tool, arguments, cancellationToken);

            string status = result.IsError is true
                ? string.Format(CultureInfo.CurrentCulture, "工具 {0} 返回错误。", toolViewModel.Name)
                : string.Format(CultureInfo.CurrentCulture, "工具 {0} 调用完成。", toolViewModel.Name);
            string resultText = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });

            StatusText = status;
            toolViewModel.CompleteInvocation(status, resultText);
        }
        catch (OperationCanceledException)
        {
            StatusText = string.Format(CultureInfo.CurrentCulture, "工具 {0} 的调用已停止。", toolViewModel.Name);
            toolViewModel.CompleteInvocation("调用已停止。", toolViewModel.ResultText);
        }
        catch (Exception ex)
        {
            string status = string.Format(CultureInfo.CurrentCulture, "工具 {0} 调用失败。", toolViewModel.Name);
            StatusText = status;
            toolViewModel.CompleteInvocation(status, ex.Message);
        }
        finally
        {
            ConnectCommand.NotifyCanExecuteChanged();
            CallToolCommand.NotifyCanExecuteChanged();
            StopToolCommand.NotifyCanExecuteChanged();
        }
    }

    private Task StopToolAsync(object? parameter)
    {
        McpToolViewModel? toolViewModel = ResolveTool(parameter);
        if (toolViewModel is null)
        {
            return Task.CompletedTask;
        }

        toolViewModel.CancelInvocation();
        StatusText = string.Format(CultureInfo.CurrentCulture, "已向工具 {0} 发送停止请求。", toolViewModel.Name);
        return Task.CompletedTask;
    }

    private McpToolViewModel? ResolveTool(object? parameter)
    {
        return parameter as McpToolViewModel ?? SelectedTool;
    }

    private void RegisterTool(McpToolViewModel tool)
    {
        ArgumentNullException.ThrowIfNull(tool);

        tool.PropertyChanged += OnToolPropertyChanged;
        _allTools.Add(tool);
    }

    private void ApplyToolFilter()
    {
        string keyword = SearchText.Trim();

        IEnumerable<McpToolViewModel> filteredTools = string.IsNullOrWhiteSpace(keyword)
            ? _allTools
            : _allTools.Where(tool =>
                tool.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || (!string.IsNullOrWhiteSpace(tool.Description)
                    && tool.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase)));

        Tools.Clear();
        foreach (McpToolViewModel tool in filteredTools)
        {
            Tools.Add(tool);
        }

        if (SelectedTool is null || !Tools.Contains(SelectedTool))
        {
            SelectedTool = Tools.FirstOrDefault();
        }

        OnPropertyChanged(nameof(HasTools));
        OnPropertyChanged(nameof(HasNoTools));
    }

    private CancellationTokenSource ResetConnectCancellation()
    {
        _connectCancellationTokenSource?.Cancel();
        _connectCancellationTokenSource?.Dispose();
        _connectCancellationTokenSource = new CancellationTokenSource();
        return _connectCancellationTokenSource;
    }

    private void OnToolPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(McpToolViewModel.IsCalling))
        {
            ConnectCommand.NotifyCanExecuteChanged();
            CallToolCommand.NotifyCanExecuteChanged();
            StopToolCommand.NotifyCanExecuteChanged();
        }
    }
}
