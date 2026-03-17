using Avalonia.Controls;
using Avalonia.Interactivity;

using SimpleWrite.Business.SimpleWriteConfigurations;
using SimpleWrite.ViewModels;

using System;

using AvaloniaAgentLib.Core;
using AvaloniaAgentLib.Model;
using AvaloniaAgentLib.ViewModel;

namespace SimpleWrite.Views.Components;

public partial class RightSlideBar : UserControl
{
    public RightSlideBar()
    {
        InitializeComponent();

        Loaded += OnLoaded;
        DataContextChanged += OnDataContextChanged;
    }

    public SimpleWriteMainViewModel MainViewModel => (SimpleWriteMainViewModel) DataContext!;

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        var dataContext = DataContext;
        if (dataContext is SimpleWriteMainViewModel mainViewModel)
        {
            var configurationManager = mainViewModel.ConfigurationManager;
            var appConfigurator = configurationManager.AppConfigurator;
            var agentApiConfiguration = appConfigurator.Of<AgentApiConfiguration>();

            CopilotViewModel copilotViewModel = CopilotSlideBar.ViewModel;

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

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {

    }
}