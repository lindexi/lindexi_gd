using System.Text;

namespace WinRemoteShell.Tests;

internal sealed class ObservableTextWriter : TextWriter
{
    private readonly object _syncRoot = new();
    private readonly StringBuilder _content = new();
    private TaskCompletionSource _contentChanged = CreateCompletionSource();

    public override Encoding Encoding => Encoding.UTF8;

    public override Task WriteAsync(char value)
    {
        Append(value.ToString());
        return Task.CompletedTask;
    }

    public override Task WriteAsync(string? value)
    {
        if (value is not null)
        {
            Append(value);
        }

        return Task.CompletedTask;
    }

    public override Task WriteAsync(char[] buffer, int index, int count)
    {
        Append(new string(buffer, index, count));
        return Task.CompletedTask;
    }

    public async Task WaitForTextAsync(string expected, TimeSpan timeout)
    {
        using var timeoutSource = new CancellationTokenSource(timeout);
        while (true)
        {
            Task contentChanged;
            string content;
            lock (_syncRoot)
            {
                content = _content.ToString();
                if (content.Contains(expected, StringComparison.Ordinal))
                {
                    return;
                }

                contentChanged = _contentChanged.Task;
            }

            try
            {
                await contentChanged.WaitAsync(timeoutSource.Token);
            }
            catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested)
            {
                throw new AssertFailedException($"Expected output to contain '{expected}', but received: {content}");
            }
        }
    }

    public override string ToString()
    {
        lock (_syncRoot)
        {
            return _content.ToString();
        }
    }

    private void Append(string value)
    {
        TaskCompletionSource contentChanged;
        lock (_syncRoot)
        {
            _content.Append(value);
            contentChanged = _contentChanged;
            _contentChanged = CreateCompletionSource();
        }

        contentChanged.TrySetResult();
    }

    private static TaskCompletionSource CreateCompletionSource() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}
