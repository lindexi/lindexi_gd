using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace XiaoXiIme.ImeUi.Avalonia;

internal static class ProductValidationBounds
{
    internal static void Start(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var path = Environment.GetEnvironmentVariable("XIAOXIIME_VALIDATION_BOUNDS");
        if (string.IsNullOrWhiteSpace(path)) return;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        timer.Tick += (_, _) =>
        {
            var controls = desktop.Windows.Where(window => window.IsVisible).SelectMany(window =>
                window.GetVisualDescendants().OfType<Control>()
                    .Where(control => control is Button or CheckBox or TextBlock && control.IsVisible)
                    .Select(control =>
                    {
                        var point = control.PointToScreen(new global::Avalonia.Point(control.Bounds.Width / 2, control.Bounds.Height / 2));
                        return new { title = window.Title, text = control is TextBlock textBlock ? textBlock.Text : (control as ContentControl)?.Content?.ToString(), x = point.X, y = point.Y, enabled = control.IsEnabled };
                    })).ToArray();
            var temporary = path + ".tmp";
            var settings = desktop.Windows.OfType<ProductSettingsWindow>().Where(window => window.IsVisible).Select(window => window.ValidationState).ToArray();
            File.WriteAllText(temporary, JsonSerializer.Serialize(new { pid = Environment.ProcessId, controls, settings }));
            File.Move(temporary, path, true);
        };
        desktop.Exit += (_, _) => timer.Stop();
        timer.Start();
    }
}
