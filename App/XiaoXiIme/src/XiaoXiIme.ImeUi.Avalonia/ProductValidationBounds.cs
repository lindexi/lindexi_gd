using System.Text.Json;
using System.Threading.Channels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace XiaoXiIme.ImeUi.Avalonia;

internal static class ProductValidationBounds
{
    internal static void Start(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var path = Environment.GetEnvironmentVariable("XIAOXIIME_VALIDATION_BOUNDS");
        if (string.IsNullOrWhiteSpace(path)) return;
        var attached = new HashSet<Window>();
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        long generation = 0;
        long pointerSequence = 0;
        Exception? publishFailure = null;
        var writes = Channel.CreateUnbounded<(string Path, string Content, bool Snapshot)>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });
        var writer = Task.Run(async () =>
        {
            try
            {
                await foreach (var write in writes.Reader.ReadAllAsync().ConfigureAwait(false))
                {
                    if (write.Snapshot)
                    {
                        var temporary = write.Path + ".tmp";
                        await File.WriteAllTextAsync(temporary, write.Content).ConfigureAwait(false);
                        using var sampled = JsonDocument.Parse(write.Content);
                        await File.AppendAllTextAsync(path + ".writer.jsonl", JsonSerializer.Serialize(new
                        {
                            time = DateTimeOffset.UtcNow,
                            sampledAt = sampled.RootElement.GetProperty("time").GetDateTimeOffset(),
                            generation = sampled.RootElement.GetProperty("generation").GetInt64(),
                            thread = Environment.CurrentManagedThreadId
                        }) + Environment.NewLine).ConfigureAwait(false);
                        if (File.Exists(write.Path))
                            File.Replace(temporary, write.Path, null);
                        else
                            File.Move(temporary, write.Path);
                    }
                    else
                        await File.AppendAllTextAsync(write.Path, write.Content).ConfigureAwait(false);
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException)
            {
                Volatile.Write(ref publishFailure, exception);
                Console.Error.WriteLine(exception.ToString());
                try
                {
                    await File.WriteAllTextAsync(path + ".error.txt", exception.ToString()).ConfigureAwait(false);
                }
                catch (IOException reportException) { Console.Error.WriteLine(reportException.ToString()); }
                catch (UnauthorizedAccessException reportException) { Console.Error.WriteLine(reportException.ToString()); }
            }
            finally
            {
                Console.Error.WriteLine($"bounds-writer-exit generation={generation} failed={Volatile.Read(ref publishFailure) is not null}");
            }
        });
        var handlers = new Dictionary<Window, EventHandler<RoutedEventArgs>>();
        object Identity(object? value) => value is Control control
            ? new { type = (string?)control.GetType().Name, name = control.Name }
            : new { type = value?.GetType().Name, name = (string?)null };
        void Trace(Window window, RoutedEventArgs args)
        {
            var pointer = args as PointerEventArgs;
            var client = pointer?.GetPosition(window);
            var screen = client is { } point ? window.PointToScreen(point) : (PixelPoint?)null;
            var source = args.Source as Control;
            var button = source as Button ?? source?.GetVisualAncestors().OfType<Button>().FirstOrDefault();
            writes.Writer.TryWrite((path + ".pointer.jsonl", JsonSerializer.Serialize(new
            {
                time = DateTimeOffset.UtcNow, pid = Environment.ProcessId, generation, sequence = ++pointerSequence,
                handle = window.TryGetPlatformHandle()?.Handle.ToInt64(), window = window.GetType().Name,
                eventName = args.RoutedEvent?.Name, route = args.Route.ToString(), args.Handled,
                source = Identity(args.Source), target = Identity(button), client, screen,
                hit = Identity(client is { } p ? window.InputHitTest(p) : null),
                captured = Identity(pointer?.Pointer.Captured), pressed = button?.IsPressed,
                enabled = button?.IsEffectivelyEnabled, active = window.IsActive,
                page = (window as DictionaryDebugWindow)?.ValidationPage
            }) + Environment.NewLine, false));
        }
        timer.Tick += (_, _) =>
        {
            if (Volatile.Read(ref publishFailure) is not null)
            {
                timer.Stop();
                Console.Error.WriteLine($"bounds-timer-stopped writer-failure generation={generation}");
                return;
            }
            try
            {
            var sampleStarted = System.Diagnostics.Stopwatch.GetTimestamp();
            SettingsApplyTrace.Mark("snapshot-start", sampleStarted);
            generation++;
            foreach (var window in desktop.Windows.Where(w => attached.Add(w)))
            {
                EventHandler<RoutedEventArgs> handler = (_, args) => Trace(window, args);
                handlers.Add(window, handler);
                window.AddHandler(InputElement.PointerPressedEvent, handler, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, true);
                window.AddHandler(InputElement.PointerReleasedEvent, handler, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, true);
                window.AddHandler(Button.ClickEvent, handler, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, true);
            }
            var controls = desktop.Windows.Where(window => window.IsVisible).SelectMany(window =>
                window.GetVisualDescendants().OfType<Control>()
                    .Where(control => control is Button or CheckBox or TextBlock or TextBox && control.IsVisible)
                    .Select(control =>
                    {
                        var local = new Point(control is CheckBox ? Math.Min(16, control.Bounds.Width / 2) : control.Bounds.Width / 2, control.Bounds.Height / 2);
                        var point = control.PointToScreen(local);
                        var client = window.PointToClient(point);
                        var hit = window.InputHitTest(client) as Control;
                        var origin = control.PointToScreen(new Point(0, 0));
                        var corner = control.PointToScreen(new Point(control.Bounds.Width, control.Bounds.Height));
                        var hittable = hit == control || hit?.GetVisualAncestors().Contains(control) == true;
                        return new { name = control.Name, automationId = global::Avalonia.Automation.AutomationProperties.GetAutomationId(control), left = origin.X, top = origin.Y, width = corner.X - origin.X, height = corner.Y - origin.Y, title = window.Title, handle = window.TryGetPlatformHandle()?.Handle.ToInt64(), type = control.GetType().Name, focused = control.IsKeyboardFocusWithin, text = control is TextBlock textBlock ? textBlock.Text : (control as ContentControl)?.Content?.ToString(), x = point.X, y = point.Y, enabled = control.IsEffectivelyEnabled, hittable, client, hit = Identity(hit), visualRoot = Identity(control.GetVisualRoot()), bounds = control.Bounds };
                    })).ToArray();
            SettingsApplyTrace.Mark("snapshot-controls", sampleStarted);
            var windows = desktop.Windows.Select(w => new { type = w.GetType().Name, w.IsActive, w.IsVisible, w.ShowActivated, w.Bounds, w.ClientSize, w.Position, w.RenderScaling, handle = w.TryGetPlatformHandle()?.Handle.ToInt64() }).ToArray();
            var settings = desktop.Windows.OfType<ProductSettingsWindow>().Where(window => window.IsVisible).Select(window => window.ValidationState).ToArray();
            writes.Writer.TryWrite((path, JsonSerializer.Serialize(new { time = DateTimeOffset.UtcNow, pid = Environment.ProcessId, generation, pointerSequence, controls, windows, settings, debug = desktop.Windows.OfType<DictionaryDebugWindow>().Select(w => w.ValidationState).ToArray() }, new JsonSerializerOptions { WriteIndented = true, NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals }), true));
            }
            catch (Exception exception) when (exception is InvalidOperationException or ArgumentException or JsonException or IOException or UnauthorizedAccessException)
            {
                Volatile.Write(ref publishFailure, exception);
                timer.Stop();
                writes.Writer.TryComplete();
                Console.Error.WriteLine($"bounds-timer-stopped sample-failure generation={generation}: {exception}");
            }
        };
        desktop.Exit += (_, _) =>
        {
            timer.Stop();
            Console.Error.WriteLine($"bounds-timer-stopped application-exit generation={generation} writerStatus={writer.Status}");
            foreach (var (window, handler) in handlers)
            {
                window.RemoveHandler(InputElement.PointerPressedEvent, handler);
                window.RemoveHandler(InputElement.PointerReleasedEvent, handler);
                window.RemoveHandler(Button.ClickEvent, handler);
            }
            handlers.Clear();
            attached.Clear();
            writes.Writer.TryComplete();
            writer.GetAwaiter().GetResult();
        };
        timer.Start();
    }
}
