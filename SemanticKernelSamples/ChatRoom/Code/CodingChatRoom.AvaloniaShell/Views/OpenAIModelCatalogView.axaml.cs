using System;
using System.Diagnostics;
using System.Linq;

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

using CodingChatRoom.AvaloniaShell.Services;
using CodingChatRoom.AvaloniaShell.ViewModels;

namespace CodingChatRoom.AvaloniaShell.Views;

public partial class OpenAIModelCatalogView : UserControl
{
    private readonly IOpenAIModelCatalogClient _client = new OpenAIModelCatalogClient();

    public OpenAIModelCatalogView()
    {
        InitializeComponent();
    }

    public async void LoadModels(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ProviderSettingsViewModel provider || sender is not Button button)
        {
            return;
        }

        button.IsEnabled = false;
        CatalogPanel.IsVisible = true;
        ModelsList.IsVisible = false;
        ShowStatus("正在获取模型列表…", isError: false);
        try
        {
            OpenAIModelCatalogResult result = await _client.GetModelIdsAsync(provider.EndPoint, provider.ApiKey);
            if (!result.IsSuccessful)
            {
                ModelsList.ItemsSource = null;
                ShowStatus(result.ErrorMessage!, isError: true);
                return;
            }

            ModelsList.ItemsSource = result.ModelIds;
            ModelsList.IsVisible = result.ModelIds.Count > 0;
            StatusText.IsVisible = result.ModelIds.Count == 0;
            StatusText.Text = result.ModelIds.Count == 0 ? "没有可导入的模型。" : null;
        }
        catch (Exception exception)
        {
            Trace.TraceError($"导入 OpenAI 模型列表失败：{exception}");
            ModelsList.ItemsSource = null;
            ShowStatus("获取模型列表时发生意外错误。请检查日志并重试。", isError: true);
        }
        finally
        {
            button.IsEnabled = true;
        }
    }

    private void OnAddClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ProviderSettingsViewModel provider
            || sender is not Button { DataContext: string modelId } button
            || IsConfigured(provider, modelId))
        {
            return;
        }

        ModelSettingsViewModel? model = provider.Models.FirstOrDefault(model =>
            string.IsNullOrWhiteSpace(model.Provider)
            && string.IsNullOrWhiteSpace(model.ModelName)
            && string.IsNullOrWhiteSpace(model.ModelId));
        if (model is null)
        {
            model = new ModelSettingsViewModel();
            provider.Models.Add(model);
        }

        model.Provider = "openai";
        model.ModelName = modelId;
        model.ModelId = modelId;
        if (button.Parent?.Parent is Border modelItem)
        {
            modelItem.IsVisible = false;
        }
    }

    private void OnModelItemLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ProviderSettingsViewModel provider
            && sender is Border { DataContext: string modelId } modelItem)
        {
            modelItem.IsVisible = !IsConfigured(provider, modelId);
        }
    }

    private static bool IsConfigured(ProviderSettingsViewModel provider, string modelId)
        => provider.Models.Any(model => model.ModelId == modelId || model.ModelName == modelId);

    private void ShowStatus(string message, bool isError)
    {
        StatusText.Text = message;
        StatusText.Foreground = isError ? Brushes.Crimson : Brushes.Gray;
        StatusText.IsVisible = true;
    }
}
