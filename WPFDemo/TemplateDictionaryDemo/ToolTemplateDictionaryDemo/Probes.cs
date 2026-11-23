using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml;
using Ava = Avalonia.Controls;
using Wpf = System.Windows.Controls;

namespace ToolTemplateDictionaryDemo;

public sealed record ToolItem(string ToolName, string OutputText);

public partial class AvaloniaProbe : Ava.UserControl
{
    public AvaloniaProbe() => AvaloniaXamlLoader.Load(this);
}

public partial class WpfProbe : Wpf.UserControl
{
    public WpfProbe() => InitializeComponent();
}

public sealed class AvaloniaSelector : IDataTemplate
{
    public Dictionary<string, IDataTemplate> ToolItemTemplates { get; } = new(StringComparer.Ordinal);

    public bool Match(object? data) => data is ToolItem;

    public Ava.Control? Build(object? param) => param is ToolItem tool
        ? ToolItemTemplates.GetValueOrDefault(tool.ToolName)?.Build(param)
        : null;
}

public sealed class WpfSelector : Wpf.DataTemplateSelector
{
    public Dictionary<string, System.Windows.DataTemplate> ToolItemTemplates { get; } = new(StringComparer.Ordinal);

    public override System.Windows.DataTemplate? SelectTemplate(object item, System.Windows.DependencyObject container) =>
        item is ToolItem tool ? ToolItemTemplates.GetValueOrDefault(tool.ToolName) : null;
}
