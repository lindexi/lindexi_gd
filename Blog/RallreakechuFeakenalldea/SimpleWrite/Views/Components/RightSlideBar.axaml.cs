using Avalonia.Controls;
using Avalonia.Animation;
using Avalonia.Interactivity;
using Avalonia.Input;

using AvaloniaAgentLib.Core;
using AvaloniaAgentLib.Logging;
using AvaloniaAgentLib.Model;
using AvaloniaAgentLib.ViewModel;

using SimpleWrite.Business;
using SimpleWrite.Business.SimpleWriteConfigurations;
using SimpleWrite.ViewModels;

using System;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using SimpleWrite.Business.TextEditors.CommandPatterns;

namespace SimpleWrite.Views.Components;

public partial class RightSlideBar : UserControl
{
    private const double DefaultExpandedWidth = 300;
    private const double CollapsedWidth = 2;
    private const double MinimumExpandedWidth = 160;

    private bool _isExpanded = true;
    private bool _isInitialized;
    private bool _isResizing;
    private double _expandedWidth = DefaultExpandedWidth;
    private double _resizeStartPointerX;
    private double _resizeStartWidth;
    private Transitions? _widthTransitions;

    public RightSlideBar()
    {
        InitializeComponent();
        _widthTransitions = Transitions;

        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
    }

    public SimpleWriteMainViewModel MainViewModel => (SimpleWriteMainViewModel) DataContext!;

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (_isInitialized)
        {
            return;
        }

        if (!double.IsNaN(Width) && Width > CollapsedWidth)
        {
            _expandedWidth = Width;
        }

        ApplySidebarState(isExpanded: true, storeCurrentWidth: false);
        _isInitialized = true;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        var dataContext = DataContext;
        if (dataContext is SimpleWriteMainViewModel mainViewModel)
        {
            var configurationManager = mainViewModel.ConfigurationManager;
            var appConfigurator = configurationManager.AppConfigurator;
            var agentApiConfiguration = appConfigurator.Of<AgentApiConfiguration>();

            CopilotViewModel copilotViewModel = CopilotSlideBar.ViewModel;
            copilotViewModel.ChatLogger = new FileCopilotChatLogger(mainViewModel.AppPathManager.CopilotChatLogDirectory);

            const string endPointHelpText = "填充 OpenAI 兼容 API 的地址，如  https://ark.cn-beijing.volces.com/api/v3";
            const string keyHelpText = "请填充密码";
            const string modelNameHelpText = "请填充模型名";

            if (IsIsInvalid())
            {
                agentApiConfiguration.EndPoint ??= endPointHelpText;
                agentApiConfiguration.Key ??= keyHelpText;
                agentApiConfiguration.ModelName ??= modelNameHelpText;

                copilotViewModel.ChatMessages.Add(CopilotChatMessage.CreateAssistant($"请点击设置，设置模型的连接", isPresetInfo: true));
            }
            else
            {
                copilotViewModel.AgentApiEndpointManager.CurrentEndpoint = new ApiEndpoint(
                    agentApiConfiguration.EndPoint, agentApiConfiguration.Key, agentApiConfiguration.ModelName);

                var copilotPatternProvider = new CopilotPatternProvider(copilotViewModel);
                copilotPatternProvider.AddCopilotPatterns(mainViewModel.CommandPatternManager);
            }

            copilotViewModel.SettingOpened -= CopilotViewModel_OnSettingOpened;
            copilotViewModel.SettingOpened += CopilotViewModel_OnSettingOpened;

            bool IsIsInvalid()
            {
                if (string.IsNullOrEmpty(agentApiConfiguration.EndPoint)
                    || string.IsNullOrEmpty(agentApiConfiguration.Key)
                    || string.IsNullOrEmpty(agentApiConfiguration.ModelName))
                {
                    return true;
                }

                if (agentApiConfiguration.EndPoint == endPointHelpText)
                {
                    return true;
                }

                string endPoint = agentApiConfiguration.EndPoint;
                if (!endPoint.StartsWith("http"))
                {
                    return true;
                }

                if (agentApiConfiguration.Key == keyHelpText)
                {
                    return true;
                }

                if (agentApiConfiguration.ModelName == modelNameHelpText)
                {
                    return true;
                }

                return false;
            }
        }
    }

    private void CopilotViewModel_OnSettingOpened(object? sender, EventArgs e)
    {
        var applicationConfigurationFile = MainViewModel.AppPathManager.ApplicationConfigurationFile;
        _ = MainViewModel.OpenFileAsync(applicationConfigurationFile);
    }

    private void ToggleSidebarButton_OnClick(object? sender, RoutedEventArgs e)
    {
        ApplySidebarState(!_isExpanded);
    }

    private void ResizeHandleBorder_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!_isExpanded || sender is not Control resizeHandle || e.GetCurrentPoint(resizeHandle).Properties.IsLeftButtonPressed == false)
        {
            return;
        }

        _isResizing = true;
        _resizeStartPointerX = GetPointerX(e);
        _resizeStartWidth = Width;
        _widthTransitions ??= this.Transitions;
        Transitions = null;

        e.Pointer.Capture(resizeHandle);
        e.Handled = true;
    }

    private void ResizeHandleBorder_OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isResizing)
        {
            return;
        }

        double currentPointerX = GetPointerX(e);
        double deltaX = currentPointerX - _resizeStartPointerX;
        double newWidth = Math.Max(MinimumExpandedWidth, _resizeStartWidth - deltaX);

        Width = newWidth;
        _expandedWidth = newWidth;
        e.Handled = true;
    }

    private void ResizeHandleBorder_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isResizing)
        {
            return;
        }

        EndResize(e.Pointer);
        e.Handled = true;
    }

    private void ResizeHandleBorder_OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (!_isResizing)
        {
            return;
        }

        EndResize(pointer: null);
    }

    private void ApplySidebarState(bool isExpanded, bool storeCurrentWidth = true)
    {
        if (!isExpanded && storeCurrentWidth && !double.IsNaN(Width) && Width > CollapsedWidth)
        {
            _expandedWidth = Width;
        }

        _isExpanded = isExpanded;

        SidebarContentHost.IsVisible = isExpanded;
        Width = isExpanded ? _expandedWidth : CollapsedWidth;
        ToggleChevronTextBlock.Text = isExpanded ? "❯" : "❮";
        ToggleSidebarButton.Margin = isExpanded ? new Thickness(0, 0, 0, 0) : new Thickness(-35, 0, 0, 0);
        ToolTip.SetTip(ToggleSidebarButton, isExpanded ? "收起侧边栏" : "展开侧边栏");
    }

    private void EndResize(IPointer? pointer)
    {
        _isResizing = false;
        pointer?.Capture(null);
        Transitions = _widthTransitions;
    }

    private double GetPointerX(PointerEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        return topLevel is null ? e.GetPosition(this).X : e.GetPosition(topLevel).X;
    }
}

file class CopilotPatternProvider(CopilotViewModel copilotViewModel)
{
    private static readonly UTF8Encoding Utf8Encoding = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public void AddCopilotPatterns(CommandPatternManager commandPatternManager)
    {
        ArgumentNullException.ThrowIfNull(commandPatternManager);

        commandPatternManager.AddCommandPattern("发送选中内容到 Copilot 聊天", text => copilotViewModel.SendMessageAsync(text, withHistory: false));

        commandPatternManager.AddCommandPattern("翻译为计算机英文", text =>
        {
            var prompt =
                     $"""
                     请帮我将以下内容转述为地道的计算机英文，我将在即时聊天中使用：
                     {text}
                     """;
            return copilotViewModel.SendMessageAsync(prompt, withHistory: false);
        });

        commandPatternManager.AddCommandPattern("Json转C#类", text =>
        {
            var prompt =
                    $"""
                     将以下 json 转换为 C# 的类型，要求使用 System.Text.Json 作为 Json 特性定义。要求 C# 属性命名符合 .NET 规范，采用帕斯卡风格：
                     {text}
                     """;
            return copilotViewModel.SendMessageAsync(prompt, withHistory: false);
        }, supportSingleLine: false);

        commandPatternManager.AddCommandPattern("文本转 Base64", text =>
        {
            string result = Convert.ToBase64String(Utf8Encoding.GetBytes(text));
            return AddLocalConversionAsync("请将以下内容转换为 Base64：", text, result);
        });

        commandPatternManager.AddCommandPattern("Base64 转文本", text =>
        {
            _ = TryDecodeBase64(text, out string result);
            return AddLocalConversionAsync("请将以下 Base64 内容转换为文本：", text, result);
        }, isMatchFunc: static text => ValueTask.FromResult(TryDecodeBase64(text, out _)));

        commandPatternManager.AddCommandPattern("文本转二进制", text =>
        {
            string result = ConvertTextToBinary(text);
            return AddLocalConversionAsync("请将以下文本转换为二进制（UTF-8）：", text, result);
        });

        commandPatternManager.AddCommandPattern("二进制转文本", text =>
        {
            _ = TryDecodeBinaryText(text, out string result);
            return AddLocalConversionAsync("请将以下二进制内容转换为文本：", text, result);
        }, isMatchFunc: static text => ValueTask.FromResult(TryDecodeBinaryText(text, out _)));
    }

    private Task AddLocalConversionAsync(string instruction, string text, string result)
    {
        var prompt =
            $"""
             {instruction}
             {text}
             """;

        var response =
            $"""
             结果：
             {result}
             """;

        return copilotViewModel.AddLocalConversationAsync(prompt, response);
    }

    private static bool TryDecodeBase64(string text, out string result)
    {
        result = string.Empty;

        string normalizedText = RemoveWhitespace(text);
        if (string.IsNullOrWhiteSpace(normalizedText) || normalizedText.Length % 4 != 0)
        {
            return false;
        }

        byte[] buffer = new byte[normalizedText.Length];
        if (!Convert.TryFromBase64String(normalizedText, buffer, out int bytesWritten))
        {
            return false;
        }

        try
        {
            result = Utf8Encoding.GetString(buffer, 0, bytesWritten);
            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }

    private static string ConvertTextToBinary(string text)
    {
        byte[] byteArray = Utf8Encoding.GetBytes(text);
        var stringBuilder = new StringBuilder(byteArray.Length * 9);
        for (int i = 0; i < byteArray.Length; i++)
        {
            if (i > 0)
            {
                stringBuilder.Append(' ');
            }

            stringBuilder.Append(Convert.ToString(byteArray[i], 2).PadLeft(8, '0'));
        }

        return stringBuilder.ToString();
    }

    private static bool TryDecodeBinaryText(string text, out string result)
    {
        result = string.Empty;

        string normalizedText = RemoveWhitespace(text);
        if (string.IsNullOrWhiteSpace(normalizedText) || normalizedText.Length % 8 != 0)
        {
            return false;
        }

        byte[] byteArray = new byte[normalizedText.Length / 8];
        for (int i = 0; i < normalizedText.Length; i += 8)
        {
            ReadOnlySpan<char> byteSpan = normalizedText.AsSpan(i, 8);
            for (int j = 0; j < byteSpan.Length; j++)
            {
                if (byteSpan[j] is not ('0' or '1'))
                {
                    return false;
                }
            }

            byteArray[i / 8] = Convert.ToByte(byteSpan.ToString(), 2);
        }

        try
        {
            result = Utf8Encoding.GetString(byteArray);
            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }

    private static string RemoveWhitespace(string text)
    {
        var stringBuilder = new StringBuilder(text.Length);
        foreach (char c in text)
        {
            if (!char.IsWhiteSpace(c))
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString();
    }
}