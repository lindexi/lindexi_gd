using System;
using System.IO;

using AgentLib.Model;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using Google.Protobuf.Compiler;

using Microsoft.Extensions.AI;

namespace PptxGenerator;

public partial class CharUserControl : UserControl
{
    public CharUserControl()
    {
        InitializeComponent();
    }

    public MainWindowViewModel ViewModel => DataContext as MainWindowViewModel ?? throw new InvalidCastException("DataContext must be of type MainWindowViewModel.");

    private async void DebugButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var copilotChatManager = ViewModel.SlideChatManager.ChatManager;
        var slideImageFile =
            @"C:\lindexi\Work\根据文档生成视频\uxots3v4.yly.png";
        if (!File.Exists(slideImageFile))
        {
            return;
        }

        var dataContent = await DataContent.LoadFromAsync(slideImageFile);

        var prompt = SlideChatManager.BuildInitialUserPrompt($"请将附图内容转换为 SlideML 进行描述");

        var slideRenderTool = ViewModel.SlideChatManager.SlideRenderTool;

        ViewModel.StatusText = "开始生成中";

        var result = copilotChatManager.SendMessage(new SendMessageRequest(
             [
                 new TextContent(prompt),
                dataContent,
            ])
        {
            SystemPrompt = SlideChatManager.BuildSystemPrompt(),
            Tools = [
                slideRenderTool.CreateTool(),
                slideRenderTool.CreatePreviewTool()
            ]
        });
        await result.RunTask;

        ViewModel.StatusText = "执行完成";
    }
}