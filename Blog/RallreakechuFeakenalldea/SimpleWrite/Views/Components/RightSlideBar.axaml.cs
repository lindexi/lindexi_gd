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
using System.Threading.Tasks;
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

                mainViewModel.CopilotHandler = new CopilotHandler(copilotViewModel);
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
        ToggleSidebarButton.Margin = isExpanded ? new Thickness(0,0, 0, 0) : new Thickness(-35, 0, 0, 0);
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

file class CopilotHandler(CopilotViewModel copilotViewModel) : ICopilotHandler
{
    public CopilotViewModel CopilotViewModel { get; } = copilotViewModel;

    public Task SendMessageToCopilotAsync(string text, bool withHistory)
    {
        return CopilotViewModel.SendMessageAsync(text, withHistory);
    }
}