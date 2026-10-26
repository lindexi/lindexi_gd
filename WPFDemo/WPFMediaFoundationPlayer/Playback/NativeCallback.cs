using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.Media.MediaFoundation;
using Windows.Win32.System.Com;

namespace MediaFoundation.Core;

internal sealed unsafe class NativeCallback
{
    private readonly object _gate = new();
    private readonly Queue<Notification> _pending = new();
    private readonly SynchronizationContext _context;
    private readonly WeakReference<MediaPlaybackSession> _owner;
    private bool _closed;
    private bool _scheduled;

    internal NativeCallback(MediaPlaybackSession session, SynchronizationContext context)
    {
        _owner = new(session);
        this._context = context;
    }

    internal readonly record struct Notification(MFP_EVENT_TYPE Type, HRESULT Result, nint Item);

    [StructLayout(LayoutKind.Sequential)]
    private struct Instance
    {
        internal IMFPMediaPlayerCallback.Vtbl* Vtable;
        internal int References;
        internal nint Handle;
    }

    private static readonly IMFPMediaPlayerCallback.Vtbl* s_vtable = CreateVtable();
    private static int s_liveInstances;

    private static IMFPMediaPlayerCallback.Vtbl* CreateVtable()
    {
        var table = (IMFPMediaPlayerCallback.Vtbl*)RuntimeHelpers.AllocateTypeAssociatedMemory
            (typeof(NativeCallback), sizeof(IMFPMediaPlayerCallback.Vtbl));
        table->QueryInterface_1 = &QueryInterface;
        table->AddRef_2 = &AddRef;
        table->Release_3 = &Release;
        table->OnMediaPlayerEvent_4 = &OnEvent;
        return table;
    }

    internal IMFPMediaPlayerCallback* Create()
    {
        var instance = (Instance*)NativeMemory.AllocZeroed((nuint)sizeof(Instance));
        instance->Vtable = s_vtable;
        instance->References = 1;
        instance->Handle = GCHandle.ToIntPtr(GCHandle.Alloc(this));
        Trace.WriteLine
            ($"MFPlay callback allocated live={Interlocked.Increment(ref s_liveInstances)}", "MediaFoundation.Core");
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
    private static uint AddRef
        (IMFPMediaPlayerCallback* self) => (uint)Interlocked.Increment(ref ((Instance*)self)->References);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static uint Release(IMFPMediaPlayerCallback* self)
    {
        var instance = (Instance*)self;
        int count = Interlocked.Decrement(ref instance->References);
        if (count == 0)
        {
            GCHandle.FromIntPtr(instance->Handle).Free();
            NativeMemory.Free(instance);
            Trace.WriteLine
                ($"MFPlay callback released live={Interlocked.Decrement(ref s_liveInstances)}", "MediaFoundation.Core");
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
        lock (_gate)
        {
            if (_closed) return;
            IMFPMediaItem* item = header->eEventType == MFP_EVENT_TYPE.MFP_EVENT_TYPE_MEDIAITEM_CREATED
                ? ((MFP_MEDIAITEM_CREATED_EVENT*)header)->pMediaItem
                : null;
            if (item != null) item->AddRef();
            _pending.Enqueue(new(header->eEventType, header->hrEvent, (nint)item));
            if (_scheduled) return;
            _scheduled = true;
            try
            {
                _context.Post(static state => ((NativeCallback)state!).Drain(), this);
            }
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
            lock (_gate)
            {
                if (_closed || _pending.Count == 0)
                {
                    _scheduled = false;
                    return;
                }

                notification = _pending.Dequeue();
            }

            try
            {
                if (_owner.TryGetTarget(out var session)) session.HandleNotification(this, notification);
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
        lock (_gate)
        {
            _closed = true;
            while (_pending.TryDequeue(out var notification))
                if (notification.Item != 0)
                    ((IMFPMediaItem*)notification.Item)->Release();
        }
    }
}