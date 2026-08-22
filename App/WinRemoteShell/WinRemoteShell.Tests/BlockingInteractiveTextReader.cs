namespace WinRemoteShell.Tests;

internal sealed class BlockingInteractiveTextReader(string firstLine) : TextReader
{
    private readonly ManualResetEventSlim _secondLineAvailable = new(false);
    private int _readCount;

    public void SubmitExit() => _secondLineAvailable.Set();

    public override ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Increment(ref _readCount) == 1)
        {
            return ValueTask.FromResult<string?>(firstLine);
        }

        _secondLineAvailable.Wait(cancellationToken);
        return ValueTask.FromResult<string?>("exit");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _secondLineAvailable.Set();
            _secondLineAvailable.Dispose();
        }

        base.Dispose(disposing);
    }
}
