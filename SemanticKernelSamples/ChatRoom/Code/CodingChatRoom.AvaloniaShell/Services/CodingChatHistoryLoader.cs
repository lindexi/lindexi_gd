using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using AgentLib.Logging;
using Avalonia.Threading;

namespace CodingChatRoom.AvaloniaShell.Services;

internal sealed class CodingChatHistoryLoader
{
    private readonly CodingChatApplication _application;

    public CodingChatHistoryLoader(CodingChatApplication application)
    {
        ArgumentNullException.ThrowIfNull(application);
        _application = application;
    }

    public async Task LoadAsync()
    {
        try
        {
            IReadOnlyList<CopilotChatSessionSummary> summaries = await _application
                .LoadSessionSummariesAsync()
                .ConfigureAwait(false);
            await Dispatcher.UIThread.InvokeAsync(
                () => _application.AddSessionSummaries(summaries),
                DispatcherPriority.Render);
        }
        catch (Exception exception)
        {
            Trace.TraceError($"CodingChatRoom 历史会话加载失败：{exception}");
        }
    }
}