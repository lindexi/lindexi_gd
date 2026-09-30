using System.Threading.Channels;
using XiaoXiIme.Dictionary;

namespace XiaoXiIme.ImeHost;

internal sealed class UserDictionarySaveCoordinator : IDisposable
{
    private readonly Channel<IReadOnlyList<UserDictionaryEntry>> _saveRequests = Channel.CreateBounded<IReadOnlyList<UserDictionaryEntry>>(
        new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false,
        });
    private readonly string _path;
    private readonly Action<string?> _reportError;
    private readonly Task _worker;

    internal UserDictionarySaveCoordinator(string path, Action<string?> reportError)
    {
        _path = path;
        _reportError = reportError;
        _worker = Task.Run(ProcessSaveRequestsAsync);
    }

    internal void RequestSave(IReadOnlyList<UserDictionaryEntry> entries)
    {
        _saveRequests.Writer.TryWrite(entries);
    }

    public void Dispose()
    {
        _saveRequests.Writer.TryComplete();
        _worker.GetAwaiter().GetResult();
    }

    private async Task ProcessSaveRequestsAsync()
    {
        await foreach (var entries in _saveRequests.Reader.ReadAllAsync().ConfigureAwait(false))
        {
            try
            {
                UserDictionaryStore.Save(_path, entries);
                _reportError(null);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                _reportError(exception.Message);
            }
        }
    }
}
