using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using XiaoXiIme.Foundation;
using XiaoXiIme.ImeInterop;
using XiaoXiIme.ImeIpc;

namespace XiaoXiIme.ImeModule;

internal static class AsyncImmInput
{
    private static readonly ConcurrentDictionary<ImeSessionId, Session> Sessions = new();
    private static readonly ConcurrentDictionary<ImeSessionId, long> SuspendedGenerations = new();
    private static readonly string ReplyPipe = $"XiaoXiIme_Input_{Environment.ProcessId}_{Guid.NewGuid():N}";
    private static readonly Lazy<XiaoXiImeIpcClient> Client = new(() => new XiaoXiImeIpcClient(ReplyPipe, Receive));
    private static readonly Channel<ImeInputEvent> Input = Channel.CreateUnbounded<ImeInputEvent>(new() { SingleReader = true });
    private static readonly Task Sender = Task.Run(SendAsync);
    [ThreadStatic] private static nint t_dispatchWindow;

    internal static bool HasPending(HImc context) =>
        Sessions.TryGetValue(ImeInputIdentity.Resolve(context), out var session) && Volatile.Read(ref session.Sent) != session.Applied;

    internal static void Invalidate(ImeSessionId id, bool suspend = false)
    {
        if (!suspend)
        {
            SuspendedGenerations.TryRemove(id, out _);
        }

        if (Sessions.TryRemove(id, out var session))
        {
            if (suspend && !session.Failed && Volatile.Read(ref session.Sent) == session.Applied && session.Results.IsEmpty)
            {
                SuspendedGenerations[id] = session.Generation;
            }
            session.Results.Clear();
        }
    }

    internal static bool Enqueue(HImc context, ImeKey key)
    {
        var id = ImeInputIdentity.Resolve(context);
        var anchor = ImeCandidateAnchorReader.Read(context, id);
        if (anchor is null || anchor.WindowHandle == 0 || anchor.WindowHandle != (long)GetFocus())
        {
            return false;
        }

        if (t_dispatchWindow == 0)
        {
            t_dispatchWindow = ImeUiWindowClass.CreateDispatchWindow();
            if (t_dispatchWindow == 0)
            {
                System.Diagnostics.Trace.TraceError($"[AsyncImmInput][CreateWindow] SessionId={id}; Win32Error={Marshal.GetLastPInvokeError()}");
                return false;
            }
        }

        var dispatchWindow = t_dispatchWindow;
        var session = Sessions.GetOrAdd(id, _ => new Session(context, (nint)anchor.WindowHandle, dispatchWindow));
        SuspendedGenerations.TryRemove(id, out var resumeGeneration);
        var request = new ImeProcessKeyRequest
            (key, id, session.Generation, Interlocked.Increment(ref session.Sent), anchor, resumeGeneration);
        return Input.Writer.TryWrite(new ImeInputEvent(ReplyPipe, request));
    }

    private static async Task SendAsync()
    {
        await foreach (var input in Input.Reader.ReadAllAsync().ConfigureAwait(false))
        {
            var request = input.Request;
            if (!Sessions.TryGetValue(request.SessionId, out var session) || session.Generation != request.Generation)
            {
                continue;
            }

            try
            {
                await Client.Value.SendInputAsync(input).ConfigureAwait(false);
                _ = CheckDeadlineAsync(session, request);
            }
            catch (Exception error) when (ImeHostBridge.IsHostUnavailable(error))
            {
                System.Diagnostics.Trace.TraceError($"[AsyncImmInput][Send] SessionId={request.SessionId}; Error={error}");
                Receive(new ImeProcessKeyResponse(new(ImeSessionSnapshot.Empty, null, true), request.SessionId, request.Generation, request.SequenceNumber), failed: true);
            }
        }
    }

    private static async Task CheckDeadlineAsync(Session session, ImeProcessKeyRequest request)
    {
        await Task.Delay(XiaoXiImeIpcOptions.DefaultRequestTimeout).ConfigureAwait(false);
        if (Volatile.Read(ref session.Received) < request.SequenceNumber &&
            Sessions.TryGetValue(request.SessionId, out var current) && ReferenceEquals(session, current))
        {
            System.Diagnostics.Trace.TraceWarning($"[AsyncImmInput][Deadline] SessionId={request.SessionId}; SequenceNumber={request.SequenceNumber}");
            Receive(new(new(ImeSessionSnapshot.Empty, null, true), request.SessionId, request.Generation, request.SequenceNumber), failed: true);
        }
    }

    private static void Receive(ImeProcessKeyResponse result) => Receive(result, failed: false);

    private static void Receive(ImeProcessKeyResponse result, bool failed)
    {
        if (!Sessions.TryGetValue(result.SessionId, out var session) || result.Generation != session.Generation)
        {
            return;
        }

        lock (session)
        {
            var sequence = result.ProcessedThroughSequence;
            if (session.Failed || (sequence > 0 && sequence <= session.Received))
            {
                return;
            }

            session.Failed = failed;
            if (sequence > 0 && !failed)
            {
                Volatile.Write(ref session.Received, sequence);
            }
            session.Results.Enqueue((result, failed));
        }
        if (!PostMessage(session.DispatchWindow, ImeUiWindowClass.ApplyInputMessage, 0, 0))
        {
            Invalidate(result.SessionId);
            System.Diagnostics.Trace.TraceError($"[AsyncImmInput][PostResult] SessionId={result.SessionId}; Win32Error={Marshal.GetLastPInvokeError()}");
        }
    }

    internal static void ApplyPending(nint window)
    {
        foreach (var (id, session) in Sessions)
        {
            if (session.DispatchWindow != window)
            {
                continue;
            }

            while (session.Results.TryDequeue(out var item))
            {
                if (!IsCurrentContext(session, id))
                {
                    Invalidate(id);
                    break;
                }

                var result = item.Result;
                if (result.ProcessedThroughSequence > 0 && result.ProcessedThroughSequence <= session.Applied)
                {
                    continue;
                }

                ImeLifecycleDiagnostics.Record("AsyncInput", session.Context.Value, result.Result.Handled ? 1 : 0,
                    $"apply sequence={result.ProcessedThroughSequence} commit={result.Result.CommitText?.Length ?? 0} failed={item.Failed}");
                ImeExports.ApplyAsyncInputResult(session.Context, id, result.Result);
                if (result.ProcessedThroughSequence > 0)
                {
                    session.Applied = result.ProcessedThroughSequence;
                }
                if (item.Failed)
                {
                    Invalidate(id);
                    break;
                }
            }
        }
    }

    private static bool IsCurrentContext(Session session, ImeSessionId id)
    {
        var foreground = new GuiThreadInfo { Size = (uint)Marshal.SizeOf<GuiThreadInfo>() };
        if (GetFocus() != session.TargetWindow || !InputTargetMethods.GetGUIThreadInfo(0, ref foreground) || foreground.Focus != session.TargetWindow)
        {
            return false;
        }

        var context = ImmGetContext(session.TargetWindow);
        if (context == 0)
        {
            return false;
        }
        try
        {
            return context == session.Context.Value && ImeInputIdentity.Resolve(session.Context) == id;
        }
        finally
        {
            ImmReleaseContext(session.TargetWindow, context);
        }
    }

    private sealed class Session(HImc context, nint targetWindow, nint dispatchWindow)
    {
        internal HImc Context { get; } = context;
        internal nint TargetWindow { get; } = targetWindow;
        internal nint DispatchWindow { get; } = dispatchWindow;
        internal long Generation { get; } = ImeInputIdentity.NextSequence();
        internal long Sent;
        internal long Applied;
        internal long Received;
        internal bool Failed;
        internal ConcurrentQueue<(ImeProcessKeyResponse Result, bool Failed)> Results { get; } = new();
    }

    [DllImport("user32.dll")]
    private static extern nint GetFocus();

    [DllImport("user32.dll", EntryPoint = "PostMessageW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(nint window, uint message, nuint wParam, nint lParam);

    [DllImport("imm32.dll")]
    private static extern nint ImmGetContext(nint window);

    [DllImport("imm32.dll")]
    private static extern int ImmReleaseContext(nint window, nint context);
}
