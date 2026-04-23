using Avalonia.Controls;
using Avalonia.Animation;
using Avalonia.Interactivity;
using Avalonia.Input;

using AvaloniaAgentLib.Core;
using AvaloniaAgentLib.Logging;
using AvaloniaAgentLib.Model;
using AvaloniaAgentLib.ViewModel;

using SimpleWrite.Business;
using SimpleWrite.Business.CopilotCommandPatterns;
using SimpleWrite.Business.SimpleWriteConfigurations;
using SimpleWrite.Business.TextEditors.CommandPatterns;
using SimpleWrite.ViewModels;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Avalonia;

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

            mainViewModel.SidebarConversationPresenter = new SidebarConversationPresenter(copilotViewModel);

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

                var copilotPatternProvider = new CopilotPatternProvider(copilotViewModel, configurationManager);
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

file sealed class SidebarConversationPresenter(CopilotViewModel copilotViewModel) : ISidebarConversationPresenter
{
    public Task ShowConversationAsync(string userText, string assistantText)
    {
        return copilotViewModel.AddLocalConversationAsync(userText, assistantText);
    }
}

file sealed class CopilotPatternProvider(CopilotViewModel copilotViewModel, ConfigurationManager configurationManager)
{
    public void AddCopilotPatterns(CommandPatternManager commandPatternManager)
    {
        ArgumentNullException.ThrowIfNull(commandPatternManager);

        commandPatternManager.AddCommandPattern(new PolishSelectedTextCommandPattern(copilotViewModel));

        commandPatternManager.AddCommandPattern("发送内容到 Copilot 聊天", text => copilotViewModel.SendMessageAsync(text, withHistory: false), priority: 200);

        commandPatternManager.AddCommandPattern("翻译为计算机英文", text =>
        {
            var prompt =
                $"""
                 请帮我将以下内容转述为地道的计算机英文，我将在即时聊天中使用：
                 {text}
                 """;
            return copilotViewModel.SendMessageAsync(prompt, withHistory: false);
        }, priority: 180);

        commandPatternManager.AddCommandPattern("Json转C#类", text =>
        {
            var prompt =
                $"""
                 将以下 json 转换为 C# 的类型，要求使用 System.Text.Json 作为 Json 特性定义。要求 C# 属性命名符合 .NET 规范，采用帕斯卡风格：
                 {text}
                 """;
            return copilotViewModel.SendMessageAsync(prompt, withHistory: false);
        }, supportSingleLine: false, priority: 160);

        AddXmlAbilityPatterns(commandPatternManager);
    }

    private void AddXmlAbilityPatterns(CommandPatternManager commandPatternManager)
    {
        var loadErrorList = new List<string>();
        foreach (var ability in CopilotAbilityLoader.Load(configurationManager, loadErrorList))
        {
            commandPatternManager.AddCommandPattern(ability.Title, text =>
            {
                string prompt = ability.CreatePrompt(text);
                return copilotViewModel.SendMessageAsync(prompt, withHistory: false);
            }, supportSingleLine: ability.SupportSingleLine, priority: ability.Priority);
        }

        if (loadErrorList.Count > 0)
        {
            string message = "以下 Copilot 能力文件未成功加载：" + Environment.NewLine + string.Join(Environment.NewLine, loadErrorList);
            copilotViewModel.ChatMessages.Add(CopilotChatMessage.CreateAssistant(message, isPresetInfo: true));
        }
    }
}

file sealed class CopilotAbilityLoader
{
    public static IEnumerable<CopilotAbilityDefinition> Load(ConfigurationManager configurationManager, List<string> loadErrorList)
    {
        ArgumentNullException.ThrowIfNull(configurationManager);
        ArgumentNullException.ThrowIfNull(loadErrorList);

        string abilityDirectory = configurationManager.GetCopilotAbilityDirectory().Path;
        Directory.CreateDirectory(abilityDirectory);

        foreach (var file in Directory.EnumerateFiles(abilityDirectory, "*.xml", SearchOption.TopDirectoryOnly)
                     .OrderBy(static file => file, StringComparer.OrdinalIgnoreCase))
        {
            CopilotAbilityDefinition? ability = TryParse(file, loadErrorList);
            if (ability is not null)
            {
                yield return ability;
            }
        }
    }

    private static CopilotAbilityDefinition? TryParse(string file, List<string> loadErrorList)
    {
        try
        {
            return Parse(file);
        }
        catch (InvalidOperationException ex)
        {
            loadErrorList.Add($"- {Path.GetFileName(file)}：{ex.Message}");
            return null;
        }
        catch (FormatException ex)
        {
            loadErrorList.Add($"- {Path.GetFileName(file)}：{ex.Message}");
            return null;
        }
        catch (XmlException ex)
        {
            loadErrorList.Add($"- {Path.GetFileName(file)}：XML 格式无效，{ex.Message}");
            return null;
        }
        catch (IOException ex)
        {
            loadErrorList.Add($"- {Path.GetFileName(file)}：读取失败，{ex.Message}");
            return null;
        }
    }

    private static CopilotAbilityDefinition Parse(string file)
    {
        var document = XDocument.Load(file, LoadOptions.PreserveWhitespace);
        XElement root = document.Root ?? throw new InvalidOperationException("XML 缺少根节点。");

        string title = ReadRequiredValue(root, nameof(CopilotAbilityDefinition.Title));
        string content = ReadRequiredValue(root, nameof(CopilotAbilityDefinition.Content));

        if (!content.Contains(CopilotAbilityDefinition.InputPlaceholder, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"`{nameof(CopilotAbilityDefinition.Content)}` 必须包含 `{CopilotAbilityDefinition.InputPlaceholder}` 占位符。");
        }

        int priority = ReadInt32Value(root, nameof(CopilotAbilityDefinition.Priority), defaultValue: 0);
        bool supportSingleLine = ReadBooleanValue(root, nameof(CopilotAbilityDefinition.SupportSingleLine), defaultValue: true);
        return new CopilotAbilityDefinition(title, content, priority, supportSingleLine);
    }

    private static string ReadRequiredValue(XElement root, string propertyName)
    {
        string? value = ReadOptionalValue(root, propertyName);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"缺少 `{propertyName}` 配置。");
        }

        return value.Trim();
    }

    private static int ReadInt32Value(XElement root, string propertyName, int defaultValue)
    {
        string? value = ReadOptionalValue(root, propertyName);
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
        {
            return result;
        }

        throw new FormatException($"`{propertyName}` 必须是整数。");
    }

    private static bool ReadBooleanValue(XElement root, string propertyName, bool defaultValue)
    {
        string? value = ReadOptionalValue(root, propertyName);
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        if (bool.TryParse(value, out bool result))
        {
            return result;
        }

        throw new FormatException($"`{propertyName}` 必须是 `true` 或 `false`。");
    }

    private static string? ReadOptionalValue(XElement root, string propertyName)
    {
        return (string?) root.Attribute(propertyName) ?? root.Element(propertyName)?.Value;
    }
}

file sealed class CopilotAbilityDefinition(string title, string content, int priority, bool supportSingleLine)
{
    public const string InputPlaceholder = "$(Input)";

    public string Title { get; } = title;

    public string Content { get; } = content;

    public int Priority { get; } = priority;

    public bool SupportSingleLine { get; } = supportSingleLine;

    public string CreatePrompt(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return Content.Replace(InputPlaceholder, input, StringComparison.Ordinal);
    }
}
