using PptxGenerator;

using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using AgentLib.Model;

using Microsoft.Extensions.AI;
using Microsoft.Win32;

namespace PptxGeneratorWpfDemo;

public partial class CharUserControl : UserControl
{
    public CharUserControl()
    {
        InitializeComponent();
    }

    public MainWindowViewModel ViewModel => DataContext as MainWindowViewModel ?? throw new InvalidCastException("DataContext must be of type MainWindowViewModel.");

    private async void DebugButton_OnClick(object sender, RoutedEventArgs e)
    {
        var copilotChatManager = ViewModel.SlideChatManager.ChatManager;
        var slideImageFile = @"C:\lindexi\Work\根据文档生成视频\uxots3v4.yly.png";
        if (!File.Exists(slideImageFile))
        {
            return;
        }

        var dataContent = await DataContent.LoadFromAsync(slideImageFile);

        var prompt = SlideChatManager.BuildInitialUserPrompt("请将附图内容转换为 SlideML 进行描述");

        var slideRenderTool = ViewModel.SlideChatManager.SlideRenderTool;

        ViewModel.StatusText = "开始生成中";

        var result = copilotChatManager.SendMessage(new SendMessageRequest(
            [
                new TextContent(prompt),
                dataContent,
            ])
        {
            SystemPrompt = SlideChatManager.BuildSystemPrompt(),
            Tools =
            [
                slideRenderTool.CreateTool(),
                slideRenderTool.CreatePreviewTool()
            ]
        });
        await result.RunTask;

        ViewModel.StatusText = "执行完成";
    }

    private async void ReduceButton_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.StatusText = "正在总结对话...";
        try
        {
            await ViewModel.SlideChatManager.ChatManager.ReduceSessionAsync();
            ViewModel.StatusText = "总结完成";
        }
        catch (Exception)
        {
            ViewModel.StatusText = "总结失败";
        }
    }

    private void AttachImageButton_OnClick(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "选择图片文件",
            Multiselect = true,
            Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp",
        };

        if (openFileDialog.ShowDialog() == true)
        {
            ViewModel.AddAttachedImageFiles(openFileDialog.FileNames);
        }
    }

    private void CloseAttachedImageButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: FileInfo fileInfo })
        {
            ViewModel.AttachedImageFiles.Remove(fileInfo);
        }
    }
}