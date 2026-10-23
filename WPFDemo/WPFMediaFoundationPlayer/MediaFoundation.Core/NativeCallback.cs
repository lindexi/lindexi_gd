using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.Media.MediaFoundation;
using Windows.Win32.System.Com;

namespace MediaFoundation.Core;

internal sealed unsafe class NativeCallback
{
    private readonly object gate = new();
    private readonly Queue<Notification> pending = new();
    private readonly SynchronizationContext context;
    private readonly WeakReference<MediaPlaybackSession> owner;
    private bool closed;
    private bool scheduled;

    internal NativeCallback(MediaPlaybackSession session, SynchronizationContext context)
    {
        owner = new(session);
        this.context = context;
    }

    internal readonly record struct Notification(MFP_EVENT_TYPE Type, HRESULT Result, nint Item);

    [StructLayout(LayoutKind.Sequential)]
    private struct Instance
    {
        internal IMFPMediaPlayerCallback.Vtbl* Vtable;
        internal int References;
        internal nint Handle;
    }

    private static readonly IMFPMediaPlayerCallback.Vtbl* Vtable = CreateVtable();
    private static int liveInstances;

    private static IMFPMediaPlayerCallback.Vtbl* CreateVtable()
    {
        var table = (IMFPMediaPlayerCallback.Vtbl*)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(NativeCallback), sizeof(IMFPMediaPlayerCallback.Vtbl));
        table->QueryInterface_1 = &QueryInterface;
        table->AddRef_2 = &AddRef;
        table->Release_3 = &Release;
        table->OnMediaPlayerEvent_4 = &OnEvent;
        return table;
    }

    internal IMFPMediaPlayerCallback* Create()
    {
        var instance = (Instance*)NativeMemory.AllocZeroed((nuint)sizeof(Instance));
        instance->Vtable = Vtable;
        instance->References = 1;
        instance->Handle = GCHandle.ToIntPtr(GCHandle.Alloc(this));
        Trace.WriteLine($"MFPlay callback allocated live={Interlocked.Increment(ref liveInstances)}", "MediaFoundation.Core");
        return (IMFPMediaPlayerCallback*)instance;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static HRESULT QueryInterface(IMFPMediaPlayerCallback* self, Guid* iid, void** result)
    {
        if (result == null) return new(unchecked((int)0x80004003));
        *result = null;
        if (iid == null) return new(unchecked((int)0x80004003));
        if (*iid != IMFPMediaPlayerCallback.IID_Guid && *iid != IUnknown.IID_Guid)
            return new(unchecked((int)0x80004002));
        Interlocked.Increment(ref ((Instance*)self)->References);
        *result = self;
        return new(0);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static uint AddRef(IMFPMediaPlayerCallback* self) => (uint)Interlocked.Increment(ref ((Instance*)self)->References);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static uint Release(IMFPMediaPlayerCallback* self)
    {
        var instance = (Instance*)self;
        int count = Interlocked.Decrement(ref instance->References);
        if (count == 0)
        {
            GCHandle.FromIntPtr(instance->Handle).Free();
            NativeMemory.Free(instance);
            Trace.WriteLine($"MFPlay callback released live={Interlocked.Decrement(ref liveInstances)}", "MediaFoundation.Core");
        }
        return (uint)count;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static void OnEvent(IMFPMediaPlayerCallback* self, MFP_EVENT_HEADER* header)
    {
        // No managed exception may cross this void-returning native ABI.
        try
        {
            var callback = (NativeCallback)GCHandle.FromIntPtr(((Instance*)self)->Handle).Target!;
            callback.Enqueue(header);
        }
        catch (Exception error)
        {
            Trace.TraceError("MFPlay callback dispatch failed: {0}", error);
        }
    }

    private void Enqueue(MFP_EVENT_HEADER* header)
    {
        lock (gate)
        {
            if (closed) return;
            IMFPMediaItem* item = header->eEventType == MFP_EVENT_TYPE.MFP_EVENT_TYPE_MEDIAITEM_CREATED
                ? ((MFP_MEDIAITEM_CREATED_EVENT*)header)->pMediaItem : null;
            if (item != null) item->AddRef();
            pending.Enqueue(new(header->eEventType, header->hrEvent, (nint)item));
            if (scheduled) return;
            scheduled = true;
            try { context.Post(static state => ((NativeCallback)state!).Drain(), this); }
            catch
            {
                Close();
                throw;
            }
        }
    }

    private void Drain()
    {
        while (true)
        {
            Notification notification;
            lock (gate)
            {
                if (closed || pending.Count == 0) { scheduled = false; return; }
                notification = pending.Dequeue();
            }
            try
            {
                if (owner.TryGetTarget(out var session)) session.HandleNotification(this, notification);
                else Close();
            }
            catch
            {
                // A host event handler must not strand queued native references when it throws.
                Close();
                throw;
            }
            finally
            {
                if (notification.Item != 0) ((IMFPMediaItem*)notification.Item)->Release();
            }
        }
    }

    internal void Close()
    {
        lock (gate)
        {
            closed = true;
            while (pending.TryDequeue(out var notification))
                if (notification.Item != 0) ((IMFPMediaItem*)notification.Item)->Release();
        }
    }
}
