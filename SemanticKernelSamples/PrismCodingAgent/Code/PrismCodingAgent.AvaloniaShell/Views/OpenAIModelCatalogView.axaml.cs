using Avalonia.Controls;
using CodingAgent.AvaloniaShell.ViewModels;

namespace CodingAgent.AvaloniaShell.Views;

public partial class OpenAIModelCatalogView : UserControl
{
    private ModelCatalogViewModel? _ownedModel;

    /// <summary>初始化可在设置及欢迎页使用的模型添加控件。</summary>
    public OpenAIModelCatalogView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => UpdateModel();
        AttachedToVisualTree += (_, _) => UpdateModel();
        DetachedFromVisualTree += (_, _) =>
        {
            _ownedModel?.Dispose();
            _ownedModel = null;
        };
    }

    private void UpdateModel()
    {
        _ownedModel?.Dispose();
        _ownedModel = DataContext is ProviderSettingsViewModel provider ? new ModelCatalogViewModel(provider) : null;
        if (_ownedModel is not null)
        {
            CatalogPanel.DataContext = _ownedModel;
        }
        else
        {
            CatalogPanel.ClearValue(DataContextProperty);
        }
    }

}
