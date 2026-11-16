using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;
using XiaoXiIme.ImeHost;
using XiaoXiIme.ImeIpc;

namespace XiaoXiIme.ImeUi.Avalonia;

internal sealed class DictionaryDebugSettings : IDisposable
{
    private readonly ProductSettingsStore store;
    private readonly DictionaryDebugWindow owner;
    private readonly string? packagePath;
    private ProductSettingsWindow? window;
    private string? scheme;
    private readonly SemaphoreSlim transaction = new(1, 1);
    private readonly CancellationTokenSource lifetime = new();
    private TaskCompletionSource? saveCompletion;
    internal Task PendingSave => saveCompletion?.Task ?? Task.CompletedTask;

    internal DictionaryDebugSettings(DictionaryDebugWindow owner, string? path, string? packagePath = null)
    {
        this.owner = owner;
        this.packagePath = packagePath;
        store = new(ResolvePath(path));
    }

    internal static string ResolvePath(string? path)
    {
        var formal = Path.GetFullPath
        (
            Path.Combine
            (
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "XiaoXiIme",
                "product-settings.json"
            )
        );
        var isolated = Path.GetFullPath
            (path ?? Path.Combine(Path.GetDirectoryName(formal)!, "debug-product-settings.json"));
        if (string.Equals(formal, isolated, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(nameof(path));
        }

        return isolated;
    }

    internal int InitialRuleMask => store.Settings.FuzzyRuleMask;
    internal int AppliedRuleMask => store.Settings.FuzzyRuleMask;
    internal string InitialScheme => ProductSettingsStore.GetScheme(store.Settings.InputScheme);

    internal void SetScheme(string value)
    {
        scheme = value;
        window?.Observe(Read());
        window?.SetAvailable(true);
    }

    private ImeProductSettingsResponse Read() => store.Read
            (scheme, "dictionaryDebug") with
        {
            RequiresHostRestart = false
        };

    internal void Open()
    {
        if (window is not null)
        {
            window.Activate();
            return;
        }

        owner.SuspendSettings();
        window = new ProductSettingsWindow(RefreshAsync, SaveAsync);
        window.Closing += (_, args) =>
        {
            if (args.CloseReason == global::Avalonia.Controls.WindowCloseReason.OwnerWindowClosing)
            {
                PrepareOwnerClose();
                args.Cancel = false;
            }
        };
        window.Closed += (_, _) =>
        {
            window = null;
            if (transaction.CurrentCount != 0)
            {
                owner.ResumeSettings();
            }
        };
        window.Observe(Read());
        window.SetAvailable(scheme is not null);
        window.Message("DebugHint");
        window.Show(owner);
    }

    private Task RefreshAsync()
    {
        window?.Observe(Read());
        window?.SetAvailable(scheme is not null);
        return Task.CompletedTask;
    }

    private async Task<bool> SaveAsync
        (ImeProductSettingsResponse baseline, ImeProductSettings draft, ProductSettingsWindow target)
    {
        if (lifetime.IsCancellationRequested || !await transaction.WaitAsync(0))
        {
            return false;
        }

        saveCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = System.Diagnostics.Stopwatch.GetTimestamp();
        SettingsApplyTrace.Mark("ui-apply-start", started);
        target.SetBusy(true);
        target.Message("Saving");
        try
        {
            var original = owner.CaptureSettingsSession();
            CompiledImeDictionary? dictionary = null;
            var nextScheme = ProductSettingsStore.GetScheme(draft.InputScheme);
            if (nextScheme != scheme)
            {
                dictionary = await Task.Run
                (
                    () => DictionaryPackageLoader.Load
                    (
                        DictionaryPackageLocations.ResolvePackageDirectory(packagePath, inputScheme: nextScheme)
                    ), lifetime.Token
                );
                if (dictionary.InputScheme != nextScheme)
                {
                    target.Message("PackageUnavailable");
                    return false;
                }

                SettingsApplyTrace.Mark("package-load-end", started);
            }

            SettingsApplyTrace.Mark("fuzzy-query-prepare-start", started);
            var prepared = await original.PrepareSettingsChangeAsync(dictionary, draft.FuzzyRuleMask, lifetime.Token);
            SettingsApplyTrace.Mark("fuzzy-query-prepare-end", started);
            lifetime.Token.ThrowIfCancellationRequested();
            SettingsApplyTrace.Mark("store-commit-start", started);
            var result = await store.SaveAsync
            (
                new(baseline.HostInstanceId, Guid.NewGuid(), baseline.Settings.Revision, draft), scheme,
                "dictionaryDebug", null
            );
            SettingsApplyTrace.Mark("store-commit-end", started);
            if (result.Error != ImeProductSettingsError.None)
            {
                target.Message(result.Error.ToString());
                return false;
            }

            lifetime.Token.ThrowIfCancellationRequested();
            if (prepared is not null)
            {
                owner.PublishSession(prepared);
            }

            SettingsApplyTrace.Mark("publish-end", started);
            global::Avalonia.Threading.Dispatcher.UIThread.Post
                (() => SettingsApplyTrace.Mark("dispatcher-response", started));
            scheme = nextScheme;
            target.Saved(Read());
            target.Message("DebugSaved");
            return true;
        }
        catch (DictionaryPackageException exception)
        {
            System.Diagnostics.Trace.WriteLine(exception);
            target.Message("PackageUnavailable");
            return false;
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested)
        {
            return false;
        }
        finally
        {
            target.SetBusy(false);
            transaction.Release();
            if (window is null && !lifetime.IsCancellationRequested)
            {
                owner.ResumeSettings();
            }

            saveCompletion.TrySetResult();
        }
    }

    internal void PrepareOwnerClose()
    {
        lifetime.Cancel();
        window?.SetBusy(false);
        window?.Close();
    }

    public void Dispose()
    {
        lifetime.Cancel();
        store.Dispose();
        lifetime.Dispose();
        transaction.Dispose();
    }
}