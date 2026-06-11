using PptxGenerator;

using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using AgentLib.Model;

using Microsoft.Win32;

namespace PptxGeneratorWpfDemo;

public partial class CharUserControl : UserControl
{
    public CharUserControl()
    {
        InitializeComponent();
    }

    public MainWindowViewModel ViewModel => DataContext as MainWindowViewModel ?? throw new InvalidCastException("DataContext must be of type MainWindowViewModel.");

    private async void ReduceButton_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.StatusText = "正在总结对话...";
        try
        {
            await ViewModel.SlideChatManager.Pipeline.ChatManager.ReduceSessionAsync();
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