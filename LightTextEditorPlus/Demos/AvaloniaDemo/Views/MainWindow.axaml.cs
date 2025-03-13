using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;

namespace LightTextEditorPlus.AvaloniaDemo.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // 后台创建，防止一开始就创建文本
        _contentCreatorList =
        [
            ("调试", () => new TextEditorDebugView()),
            ("测试", () => new TextEditorTestView()),
            ("Markdown", () => new DualEditorUserControl()),
        ];

        foreach ((string? name, Func<Control>? creator) in _contentCreatorList)
        {
            RootTabControl.Items.Add(new TabItem()
            {
                Header = name
            });
        }

        RootTabControl.SelectionChanged += SelectingItemsControl_OnSelectionChanged;

        RootTabControl.Loaded += (sender, args) => { UpdateContent(); };
    }

    private readonly List<(string Name, Func<Control> Creator)> _contentCreatorList;

    private void SelectingItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        UpdateContent();
    }

    private void UpdateContent()
    {
        TabItem? tabItem = RootTabControl.Items.OfType<TabItem>().FirstOrDefault(t=>t.IsSelected);
        if (tabItem is null)
        {
            return;
        }

        if (tabItem.Content is not null)
        {
            return;
        }

        Func<Control> creator = _contentCreatorList.First(t=>t.Name == tabItem.Header?.ToString()).Creator;
        tabItem.Content = creator();
    }
}