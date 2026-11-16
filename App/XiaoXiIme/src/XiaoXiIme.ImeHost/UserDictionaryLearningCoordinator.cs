using System.Threading.Channels;
using XiaoXiIme.Dictionary;

namespace XiaoXiIme.ImeHost;

/// <summary>Owns a bounded, best-effort learning worker independently of input sessions.</summary>
public sealed class UserDictionaryLearningCoordinator : IDisposable
{
    /// <summary>Maximum queued canonical segment contributions.</summary>
    public const int Capacity = 512;
    /// <summary>Maximum contributions held by one batch.</summary>
    public const int BatchSize = 128;
    /// <summary>Maximum synchronous shutdown wait in milliseconds.</summary>
    public const int ShutdownBudgetMilliseconds = 250;
    private readonly record struct LearningEvent(string Text, string Reading);
    private readonly Channel<LearningEvent> _requests;
    private readonly UserDictionary _dictionary;
    private readonly Func<IReadOnlyList<UserDictionaryEntry>, CancellationToken, Task> _saveAsync;
    private readonly CancellationTokenSource _stopping = new();
    private readonly Task _worker;
    private long _accepted, _dropped, _merged, _batches, _failures, _shutdownDropped;
    private string? _error;
    private int _disposed;
    private readonly object _lifetimeRoot = new();
    private bool _workerFinished;

    /// <summary>Starts the single supervised consumer; the persistence delegate must honor cancellation.</summary>
    public UserDictionaryLearningCoordinator(UserDictionary dictionary,
        Func<IReadOnlyList<UserDictionaryEntry>, CancellationToken, Task> saveAsync)
    {
        ArgumentNullException.ThrowIfNull(dictionary);
        ArgumentNullException.ThrowIfNull(saveAsync);
        _dictionary = dictionary;
        _saveAsync = saveAsync;
        _requests = Channel.CreateBounded<LearningEvent>(new BoundedChannelOptions(Capacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false,
        }, _ => Interlocked.Increment(ref _dropped));
        _worker = Task.Run(ProcessRequestsAsync);
    }

    /// <summary>Gets bounded-worker counters without waiting for the consumer.</summary>
    public (long Accepted, long Dropped, long Merged, long Batches, long Failures, long ShutdownDropped) Statistics =>
        (Interlocked.Read(ref _accepted), Interlocked.Read(ref _dropped), Interlocked.Read(ref _merged),
         Interlocked.Read(ref _batches), Interlocked.Read(ref _failures), Interlocked.Read(ref _shutdownDropped));

    /// <summary>Gets the most recent observed worker failure.</summary>
    public string? LastError => Volatile.Read(ref _error);

    /// <summary>Enqueues immutable canonical strings only; no session or candidate collection is retained.</summary>
    public bool TryEnqueue(string text, string canonicalReading)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(canonicalReading)) return false;
        if (_requests.Writer.TryWrite(new(text, canonicalReading)))
        {
            Interlocked.Increment(ref _accepted);
            return true;
        }
        Interlocked.Increment(ref _shutdownDropped);
        return false;
    }

    internal void RequestSave() => _requests.Writer.TryWrite(new("", ""));

    /// <summary>Stops accepting work and allows only the fixed shutdown budget to finish.</summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        lock (_lifetimeRoot)
        {
            if (!_workerFinished) _stopping.CancelAfter(ShutdownBudgetMilliseconds);
            _requests.Writer.TryComplete();
        }
        _worker.Wait(ShutdownBudgetMilliseconds);
    }

    private async Task ProcessRequestsAsync()
    {
        var token = _stopping.Token;
        try
        {
            while (await _requests.Reader.WaitToReadAsync(token).ConfigureAwait(false))
            {
                await Task.Delay(20, token).ConfigureAwait(false);
                var counts = new Dictionary<(string Text, string Reading), int>();
                var read = 0;
                while (read < BatchSize && _requests.Reader.TryRead(out var item))
                {
                    read++;
                    if (item.Text.Length == 0) continue;
                    var key = (item.Text, item.Reading);
                    if (counts.TryGetValue(key, out var count))
                    {
                        counts[key] = count + 1;
                        Interlocked.Increment(ref _merged);
                    }
                    else counts.Add(key, 1);
                }
                try
                {
                    token.ThrowIfCancellationRequested();
                    if (counts.Count > 0)
                    {
                        _dictionary.LearnBatch(counts.Select(p => new UserDictionaryEntry(p.Key.Text, p.Key.Reading, p.Value)).ToArray());
                        _dictionary.PrepareAndPublish();
                    }
                    token.ThrowIfCancellationRequested();
                    await _saveAsync(_dictionary.GetEntries(), token).ConfigureAwait(false);
                    Interlocked.Increment(ref _batches);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    Interlocked.Add(ref _shutdownDropped, read);
                    throw;
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or NotSupportedException)
                {
                    Volatile.Write(ref _error, exception.ToString());
                    Interlocked.Increment(ref _failures);
                    System.Diagnostics.Trace.TraceError("Background dictionary learning: {0}", exception);
                }
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            Interlocked.Add(ref _shutdownDropped, _requests.Reader.Count);
        }
        catch (Exception exception)
        {
            // This task is the supervision boundary: failures must not escape unobserved.
            Volatile.Write(ref _error, exception.ToString());
            Interlocked.Increment(ref _failures);
            _requests.Writer.TryComplete();
            System.Diagnostics.Trace.TraceError("Background dictionary worker: {0}", exception);
        }
        finally
        {
            lock (_lifetimeRoot)
            {
                _workerFinished = true;
                _stopping.Dispose();
            }
        }
    }
}
