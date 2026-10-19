using System.Text.Json;
using XiaoXiIme.Dictionary;
using XiaoXiIme.Foundation;
using XiaoXiIme.ImeIpc;

namespace XiaoXiIme.ImeHost;

/// <summary>Owns durable product preferences for Host and isolated dictionary debug mode.</summary>
public sealed class ProductSettingsStore : IDisposable
{
    private readonly FileStream _writerLock;
    private bool _disposed;
    private readonly string _path;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Guid _instanceId = Guid.NewGuid();
    private readonly Dictionary<Guid, (ImeSaveProductSettingsRequest Request, ImeProductSettingsResponse Response)> _operations = [];
    private sealed record Snapshot(ImeProductSettings Settings, Guid? LastOperationId = null);
    private Snapshot _snapshot = new(new());
    private readonly ImeProductSettingsError _loadError;
    private readonly bool _readOnly;

    /// <summary>Opens preferences with an exclusive writer lock.</summary>
    public ProductSettingsStore(string? path)
    {
        _path = Path.GetFullPath(path ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "XiaoXiIme", "product-settings.json"));
        var identity = OperatingSystem.IsWindows() ? _path.ToUpperInvariant() : _path;
        var lockName = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(identity)));
        var lockDirectory = Path.Combine(Path.GetTempPath(), "XiaoXiIme-settings-writers");
        Directory.CreateDirectory(lockDirectory);
        // Keep the lock file: deleting it could let another writer lock a different inode.
        _writerLock = new FileStream(Path.Combine(lockDirectory, lockName + ".lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        try
        {
            if (!File.Exists(_path)) return;
            using var stream = File.OpenRead(_path);
            var settings = JsonSerializer.Deserialize(stream, XiaoXiImeIpcJsonSerializerContext.Default.ImeProductSettings);
            if (settings is { SchemaVersion: not 1 })
            {
                _readOnly = true;
                _loadError = ImeProductSettingsError.Unsupported;
                return;
            }
            if (!IsValid(settings)) throw new JsonException();
            _snapshot = new(settings!);
        }
        catch (JsonException)
        {
            _loadError = ImeProductSettingsError.InvalidSettings;
            try { File.Move(_path, _path + ".corrupt-" + Guid.NewGuid().ToString("N")); }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                _readOnly = true;
                _loadError = ImeProductSettingsError.StorageUnavailable;
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _readOnly = true;
            _loadError = ImeProductSettingsError.StorageUnavailable;
        }
    }

    /// <summary>Gets the last committed preferences.</summary>
    public ImeProductSettings Settings => Volatile.Read(ref _snapshot).Settings;

    /// <summary>Reads authoritative preferences and startup status.</summary>
    public ImeProductSettingsResponse Read(string? scheme, string source, ImeProductSettingsError error = ImeProductSettingsError.None)
    {
        var snapshot = Volatile.Read(ref _snapshot);
        return new(_instanceId, snapshot.Settings, scheme, source,
            scheme is not null && scheme != GetScheme(snapshot.Settings.InputScheme), error, snapshot.LastOperationId, _loadError);
    }

    /// <summary>Writes preferences, optionally committing within a prepared runtime transaction.</summary>
    public async Task<ImeProductSettingsResponse> SaveAsync(ImeSaveProductSettingsRequest request, string? scheme, string source, string? hostBaseDirectory,
        Action<ImeProductSettings, Action>? commitRuntime = null)
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (request.HostInstanceId != _instanceId) return Read(scheme, source, ImeProductSettingsError.HostChanged);
            if (request.OperationId == Guid.Empty || !IsValid(request.Settings)) return Read(scheme, source, ImeProductSettingsError.InvalidSettings);
            if (_operations.TryGetValue(request.OperationId, out var previous))
                return previous.Request == request ? previous.Response : Read(scheme, source, ImeProductSettingsError.Conflict);
            if (_readOnly) return Read(scheme, source, _loadError);
            if (request.ExpectedRevision != Settings.Revision) return Read(scheme, source, ImeProductSettingsError.Conflict);
            if (request.Settings with { Revision = Settings.Revision } == Settings)
            {
                var unchanged = Read(scheme, source);
                _operations.Add(request.OperationId, (request, unchanged));
                return unchanged;
            }
            if (Settings.Revision == long.MaxValue) return Read(scheme, source, ImeProductSettingsError.StorageUnavailable);
            if (request.Settings.InputScheme != Settings.InputScheme)
            {
                try
                {
                    var target = GetScheme(request.Settings.InputScheme);
                    var package = await Task.Run(() => DictionaryPackageLoader.Load(DictionaryPackageLocations.ResolvePackageDirectory(inputScheme: target, baseDirectory: hostBaseDirectory))).ConfigureAwait(false);
                    if (package.InputScheme != target) return Read(scheme, source, ImeProductSettingsError.PackageUnavailable);
                }
                catch (DictionaryPackageException) { return Read(scheme, source, ImeProductSettingsError.PackageUnavailable); }
            }
            var next = request.Settings with { Revision = Settings.Revision + 1 };
            var temporary = _path + ".tmp-" + Guid.NewGuid().ToString("N");
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
                await using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.WriteThrough))
                {
                    await JsonSerializer.SerializeAsync(stream, next, XiaoXiImeIpcJsonSerializerContext.Default.ImeProductSettings).ConfigureAwait(false);
                    await stream.FlushAsync().ConfigureAwait(false);
                    stream.Flush(true);
                }
                void Commit()
                {
                    if (File.Exists(_path)) File.Replace(temporary, _path, null);
                    else File.Move(temporary, _path);
                    Volatile.Write(ref _snapshot, new(next, request.OperationId));
                }
                if (commitRuntime is null) Commit();
                else commitRuntime(next, Commit);
                var response = Read(scheme, source);
                _operations.Add(request.OperationId, (request, response));
                return response;
            }
            catch (NotSupportedException)
            {
                return Read(scheme, source, ImeProductSettingsError.Unsupported);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                return Read(scheme, source, ImeProductSettingsError.StorageUnavailable);
            }
            finally
            {
                try { File.Delete(temporary); }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                { System.Diagnostics.Debug.WriteLine(exception.GetType().Name); }
            }
        }
        finally { _gate.Release(); }
    }

    public void Dispose()
    {
        _gate.Wait();
        try
        {
            if (_disposed) return;
            _disposed = true;
            _writerLock.Dispose();
        }
        finally { _gate.Release(); }
    }

    private static bool IsValid(ImeProductSettings? settings) => settings is { SchemaVersion: 1, Revision: >= 0 }
        && settings.FuzzyRuleMask is >= 0 and <= 1023
        && Enum.IsDefined(settings.InputScheme)
        && (settings.StatusBarPosition is null || (Math.Abs((long)settings.StatusBarPosition.X) <= 1_000_000 && Math.Abs((long)settings.StatusBarPosition.Y) <= 1_000_000));

    /// <summary>Gets the existing package identifier without changing stable scheme values.</summary>
    public static string GetScheme(ImeProductInputScheme scheme) =>
        PhoneticSchemeCatalog.All.FirstOrDefault(item => item.ProductScheme == scheme)?.SchemeId
        ?? DictionaryPackageLocations.FullPinyinInputScheme;
}
