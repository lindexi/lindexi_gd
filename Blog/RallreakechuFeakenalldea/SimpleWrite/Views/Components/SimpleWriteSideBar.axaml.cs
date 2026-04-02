using Avalonia.Controls;
using Avalonia.Animation;
using Avalonia.Interactivity;
using Avalonia;

namespace SimpleWrite.Views.Components;

public partial class SimpleWriteSideBar : UserControl
{
    private const double DefaultExpandedWidth = 200;
    private const double CollapsedWidth = 2;

    private bool _isExpanded = true;
    private bool _isInitialized;
    private double _expandedWidth = DefaultExpandedWidth;

    public SimpleWriteSideBar()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

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

    private void ToggleSidebarButton_OnClick(object? sender, RoutedEventArgs e)
    {
        ApplySidebarState(!_isExpanded);
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
        ToggleChevronTextBlock.Text = isExpanded ? "❮" : "❯";
        ToggleSidebarButton.Margin = isExpanded ? new Thickness(0) : new Thickness(0, 0, -35, 0);
        ToolTip.SetTip(ToggleSidebarButton, isExpanded ? "收起侧边栏" : "展开侧边栏");
    }
}