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
            IReadOnlyList<CopilotChatSessionSummary> summaries = await Task.Run
                (() => _application.LoadSessionSummariesAsync()).ConfigureAwait(false);
            foreach (CopilotChatSessionSummary summary in summaries)
            {
                await Dispatcher.UIThread.InvokeAsync
                (
                    () => _application.AddSessionSummary(summary),
                    DispatcherPriority.Render
                );
            }

            await Dispatcher.UIThread.InvokeAsync
            (
                () => _application.RestoreInitialSessionAsync(summaries),
                DispatcherPriority.Render
            );
        }
        catch (Exception exception)
        {
            Trace.TraceError($"CodingChatRoom 历史会话加载失败：{exception}");
        }
    }
}