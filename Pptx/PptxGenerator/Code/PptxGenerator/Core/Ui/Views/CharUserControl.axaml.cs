using System;
using System.IO;
using System.Linq;

using AgentLib.Model;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;

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

    private async void ReduceButton_OnClick(object? sender, RoutedEventArgs e)
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

    private async void AttachImageButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
        {
            return;
        }

        var filePickerOptions = new FilePickerOpenOptions
        {
            Title = "选择图片文件",
            AllowMultiple = true,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("图片文件")
                {
                    Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif", "*.webp" }
                }
            }
        };

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(filePickerOptions);
        if (files is { Count: > 0 })
        {
            var filePaths = files.Select(f => f.Path.LocalPath);
            ViewModel.AddAttachedImageFiles(filePaths);
        }
    }

    private void CloseAttachedImageButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: FileInfo fileInfo })
        {
            ViewModel.AttachedImageFiles.Remove(fileInfo);
        }
    }
}