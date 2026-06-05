using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders.Fakes;
using AgentLib.Logging;
using AgentLib.Model;

using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

using AvaloniaAgentLib.ViewModel;

using SimpleWrite.Business;
using SimpleWrite.Business.AgentConnectors;
using SimpleWrite.Business.SimpleWriteConfigurations;
using SimpleWrite.Foundation;
using SimpleWrite.ViewModels;

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;

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
    private FolderExplorerViewModel? _subscribedFolderExplorerViewModel;
    private EditorViewModel? _subscribedEditorViewModel;

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
            CopilotViewModel copilotViewModel = CopilotSlideBar.ViewModel;
            copilotViewModel.ChatLogger = new FileCopilotChatLogger(
                mainViewModel.AppPathManager.CopilotChatLogDirectory,
                mainViewModel.AppPathManager.CopilotChatHistoryDirectory);

            _ = LoadConfigAsync(copilotViewModel);

            BindWorkspacePath(mainViewModel.FolderExplorerViewModel, mainViewModel.EditorViewModel, copilotViewModel);

            mainViewModel.SidebarConversationPresenter = new SidebarConversationPresenter(copilotViewModel);

            copilotViewModel.SettingOpened -= CopilotViewModel_OnSettingOpened;
            copilotViewModel.SettingOpened += CopilotViewModel_OnSettingOpened;
        }
    }

    private async Task LoadConfigAsync(CopilotViewModel copilotViewModel)
    {
        await EnsureAgentConfigurationFileExistsAsync();

        var mainViewModel = MainViewModel;

        // 加载配置文件
        var agentConfigurationFile = mainViewModel.AppPathManager.AgentConfigurationFile;
        var agentApiManagerConfiguration = await AgentApiManagerConfiguration.FromJsonFileAsync(agentConfigurationFile);
        copilotViewModel.AgentApiEndpointManager
            .RegisterLanguageModelProvider(new FakeLanguageModelProvider(new FakeChatClient()
            {
                OnGetStreamingResponseAsync = FakeGetStreamingResponseAsync
            }));
        copilotViewModel.AgentApiEndpointManager.LoadConfiguration(agentApiManagerConfiguration);

        static async IAsyncEnumerable<ChatResponseUpdate> FakeGetStreamingResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options, [EnumeratorCancellation] CancellationToken token)
        {
            try
            {
                await Task.Delay(TimeSpan.FromHours(10), token);
            }
            catch (TaskCanceledException)
            {
               yield break;
            }
        }

        // 完成加载之后，即可了解到是否完成配置。判断条件就是判断是否有模型加载进去了
        var finishConfig = copilotViewModel.AgentApiEndpointManager.GetSupportedModels().Count > 0;
        if (finishConfig)
        {
            // 完成配置，可以建立联系
            var configurationManager = mainViewModel.ConfigurationManager; var copilotPatternProvider = new CopilotPatternProvider(copilotViewModel, configurationManager);
            copilotPatternProvider.AddCopilotPatterns(mainViewModel.CommandPatternManager);

            // 加载 Skills 技能文件夹
            var skillsDirectory = Directory.CreateDirectory(Path.Join(mainViewModel.AppPathManager.CopilotAbilityDirectory.Path, "Skills"));
            copilotViewModel.AddSkillFolder(skillsDirectory);
        }
        else
        {
            // 提示可以配置
            copilotViewModel.ChatMessages.Add(CopilotChatMessage.CreateAssistant($"请点击设置，设置模型的连接。设置完成之后，重启应用", isPresetInfo: true));
        }
    }

    /// <summary>
    /// 绑定工作空间
    /// </summary>
    /// <param name="folderExplorerViewModel"></param>
    /// <param name="editorViewModel"></param>
    /// <param name="copilotViewModel"></param>
    private void BindWorkspacePath(FolderExplorerViewModel folderExplorerViewModel, EditorViewModel editorViewModel, CopilotViewModel copilotViewModel)
    {
        if (_subscribedFolderExplorerViewModel is not null)
        {
            _subscribedFolderExplorerViewModel.CurrentFolderChanged -= FolderExplorerViewModel_OnCurrentFolderChanged;
        }

        if (_subscribedEditorViewModel is not null)
        {
            _subscribedEditorViewModel.EditorModelChanged -= EditorViewModel_OnEditorModelChanged;
        }

        _subscribedFolderExplorerViewModel = folderExplorerViewModel;
        _subscribedFolderExplorerViewModel.CurrentFolderChanged += FolderExplorerViewModel_OnCurrentFolderChanged;

        _subscribedEditorViewModel = editorViewModel;
        _subscribedEditorViewModel.EditorModelChanged += EditorViewModel_OnEditorModelChanged;

        UpdateWorkspacePath(copilotViewModel, folderExplorerViewModel);
        UpdateSecondaryWorkspacePath(copilotViewModel, editorViewModel);
    }

    private void FolderExplorerViewModel_OnCurrentFolderChanged(object? sender, EventArgs e)
    {
        if (sender is not FolderExplorerViewModel folderExplorerViewModel)
        {
            return;
        }

        UpdateWorkspacePath(CopilotSlideBar.ViewModel, folderExplorerViewModel);
    }

    private void EditorViewModel_OnEditorModelChanged(object? sender, EventArgs e)
    {
        if (sender is not EditorViewModel editorViewModel)
        {
            return;
        }

        UpdateSecondaryWorkspacePath(CopilotSlideBar.ViewModel, editorViewModel);
    }

    private static void UpdateWorkspacePath(CopilotViewModel copilotViewModel, FolderExplorerViewModel folderExplorerViewModel)
    {
        copilotViewModel.WorkspacePath = folderExplorerViewModel.CurrentFolder?.FullName;
    }

    private static void UpdateSecondaryWorkspacePath(CopilotViewModel copilotViewModel, EditorViewModel editorViewModel)
    {
        var directoryName = editorViewModel.CurrentEditorModel.FileInfo?.DirectoryName;
        if (directoryName != null)
        {
            copilotViewModel.SecondaryWorkspacePath = directoryName;
        }
        else
        {
            // 比如没有保存的文件，没有路径，此时不要影响
        }
    }

    private void CopilotViewModel_OnSettingOpened(object? sender, EventArgs e)
    {
        _ = OpenSettingFileAsync();

        async Task OpenSettingFileAsync()
        {
            await EnsureAgentConfigurationFileExistsAsync();

            var agentConfigurationFile = MainViewModel.AppPathManager.AgentConfigurationFile;

            await MainViewModel.OpenFileAsync(agentConfigurationFile);
        }
    }

    private async Task EnsureAgentConfigurationFileExistsAsync()
    {
        var agentConfigurationFile = MainViewModel.AppPathManager.AgentConfigurationFile;

        if (!agentConfigurationFile.IsExists())
        {
            const string templateFileContent = AgentApiManagerConfiguration.DefaultTemplateFileContent;
            await using var streamWriter = File.CreateText(agentConfigurationFile);
            await streamWriter.WriteAsync(templateFileContent);
        }
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
        return copilotViewModel.AddConversationAsync(userText, assistantText, isPresetInfo: false);
    }
}