using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CodingChatRoom.AvaloniaShell.Abilities;

internal sealed class AbilityCatalog
{
    internal const string ProgrammingId = "programming";
    internal const string CompressionId = "compress";
    internal const string DefaultCompressionPrompt = "请完整保留用户目标、已完成工作、关键技术决策、文件变更、错误信息以及仍待完成的事项。";
    private readonly AbilityDirectoryReader _reader;
    private Task? _activeRefresh;

    public AbilityCatalog(DirectoryInfo directory)
    {
        ArgumentNullException.ThrowIfNull(directory);
        _reader = new AbilityDirectoryReader(directory);
    }

    public event EventHandler? Changed;

    public AbilityCollection Current { get; private set; } = AbilityCollection.Empty;

    public Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (_activeRefresh is { IsCompleted: false })
        {
            return _activeRefresh;
        }

        return _activeRefresh = RefreshCoreAsync(cancellationToken);
    }

    private async Task RefreshCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            AbilityCollection abilities = await _reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            Current = abilities;
            Changed?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Trace.TraceError($"刷新能力目录失败：{exception}");
        }
    }
}
