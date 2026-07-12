using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace XiaoXiIme.TsfModule;

internal static unsafe class TsfTextService
{
    [StructLayout(LayoutKind.Sequential)]
    private struct InterfaceNode
    {
        public void** Vtable;
        public TextServiceObject* Owner;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TextServiceObject
    {
        public InterfaceNode TextInputProcessor;
        public InterfaceNode KeyEventSink;
        public int ReferenceCount;
        public int ApartmentThreadId;
        public nint ThreadManager;
        public nint KeystrokeManager;
        public uint ClientId;
        public int IsActive;
        public int IsKeyEventSinkAdvised;
    }

    private static readonly void** s_textInputProcessorVtable = CreateTextInputProcessorVtable();
    private static readonly void** s_keyEventSinkVtable = CreateKeyEventSinkVtable();

    internal static int TryCreate(Guid interfaceId, nint* result)
    {
        if (result is null)
        {
            return TsfHResults.E_POINTER;
        }

        *result = 0;
        var instance = (TextServiceObject*) NativeMemory.AllocZeroed((nuint) sizeof(TextServiceObject));
        if (instance is null)
        {
            return TsfHResults.E_OUTOFMEMORY;
        }

        instance->TextInputProcessor.Vtable = s_textInputProcessorVtable;
        instance->TextInputProcessor.Owner = instance;
        instance->KeyEventSink.Vtable = s_keyEventSinkVtable;
        instance->KeyEventSink.Owner = instance;
        instance->ReferenceCount = 1;
        instance->ApartmentThreadId = TsfApartment.CurrentManagedThreadId;
        TsfComServer.AddObject();

        var hr = QueryInterfaceCore(&instance->TextInputProcessor, interfaceId, result);
        ReleaseCore(instance);
        return hr;
    }

    private static void** CreateTextInputProcessorVtable()
    {
        var vtable = AllocateVtable(5);
        vtable[0] = (void*) (delegate* unmanaged[Stdcall]<InterfaceNode*, Guid*, nint*, int>) &QueryInterface;
        vtable[1] = (void*) (delegate* unmanaged[Stdcall]<InterfaceNode*, uint>) &AddRef;
        vtable[2] = (void*) (delegate* unmanaged[Stdcall]<InterfaceNode*, uint>) &Release;
        vtable[3] = (void*) (delegate* unmanaged[Stdcall]<InterfaceNode*, nint, uint, int>) &Activate;
        vtable[4] = (void*) (delegate* unmanaged[Stdcall]<InterfaceNode*, int>) &Deactivate;
        return vtable;
    }

    private static void** CreateKeyEventSinkVtable()
    {
        var vtable = AllocateVtable(9);
        vtable[0] = (void*) (delegate* unmanaged[Stdcall]<InterfaceNode*, Guid*, nint*, int>) &QueryInterface;
        vtable[1] = (void*) (delegate* unmanaged[Stdcall]<InterfaceNode*, uint>) &AddRef;
        vtable[2] = (void*) (delegate* unmanaged[Stdcall]<InterfaceNode*, uint>) &Release;
        vtable[3] = (void*) (delegate* unmanaged[Stdcall]<InterfaceNode*, int, int>) &OnSetFocus;
        vtable[4] = (void*) (delegate* unmanaged[Stdcall]<InterfaceNode*, nint, nuint, nint, int*, int>) &OnTestKeyDown;
        vtable[5] = (void*) (delegate* unmanaged[Stdcall]<InterfaceNode*, nint, nuint, nint, int*, int>) &OnTestKeyUp;
        vtable[6] = (void*) (delegate* unmanaged[Stdcall]<InterfaceNode*, nint, nuint, nint, int*, int>) &OnKeyDown;
        vtable[7] = (void*) (delegate* unmanaged[Stdcall]<InterfaceNode*, nint, nuint, nint, int*, int>) &OnKeyUp;
        vtable[8] = (void*) (delegate* unmanaged[Stdcall]<InterfaceNode*, nint, Guid*, int*, int>) &OnPreservedKey;
        return vtable;
    }

    private static void** AllocateVtable(int slots)
    {
        var vtable = (void**) NativeMemory.Alloc((nuint) (sizeof(void*) * slots));
        if (vtable is null)
        {
            Environment.FailFast("Unable to allocate a TSF text service vtable.");
        }

        return vtable;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int QueryInterface(InterfaceNode* self, Guid* interfaceId, nint* result) =>
        TsfUnmanagedBoundary.Invoke(() =>
        {
            if (self is null || interfaceId is null)
            {
                return TsfHResults.E_POINTER;
            }

            return QueryInterfaceCore(self, *interfaceId, result);
        });

    private static int QueryInterfaceCore(InterfaceNode* self, Guid interfaceId, nint* result)
    {
        if (self is null || result is null)
        {
            return TsfHResults.E_POINTER;
        }

        *result = 0;
        var owner = self->Owner;
        if (interfaceId == TsfNative.IUnknownId || interfaceId == TsfNative.ITextInputProcessorId)
        {
            *result = (nint)(&owner->TextInputProcessor);
        }
        else if (interfaceId == TsfNative.IKeyEventSinkId)
        {
            *result = (nint)(&owner->KeyEventSink);
        }
        else
        {
            return TsfHResults.E_NOINTERFACE;
        }

        AddRefCore(owner);
        return TsfHResults.S_OK;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static uint AddRef(InterfaceNode* self)
    {
        try
        {
            return self is null ? 0 : AddRefCore(self->Owner);
        }
        catch
        {
            return 0;
        }
    }

    private static uint AddRefCore(TextServiceObject* owner) =>
        unchecked((uint) Interlocked.Increment(ref owner->ReferenceCount));

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static uint Release(InterfaceNode* self)
    {
        try
        {
            return self is null ? 0 : ReleaseCore(self->Owner);
        }
        catch
        {
            return 0;
        }
    }

    private static uint ReleaseCore(TextServiceObject* owner)
    {
        var count = Interlocked.Decrement(ref owner->ReferenceCount);
        if (count == 0)
        {
            DeactivateCore(owner, enforceApartment: false);
            NativeMemory.Free(owner);
            TsfComServer.ReleaseObject();
        }

        return count < 0 ? 0 : unchecked((uint) count);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int Activate(InterfaceNode* self, nint threadManager, uint clientId) =>
        TsfUnmanagedBoundary.Invoke(() => ActivateCore(self, threadManager, clientId));

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int Deactivate(InterfaceNode* self) =>
        TsfUnmanagedBoundary.Invoke(() => self is null ? TsfHResults.E_POINTER : DeactivateCore(self->Owner, enforceApartment: true));

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int OnSetFocus(InterfaceNode* self, int foreground) =>
        TsfUnmanagedBoundary.Invoke(() => ValidateActive(self));

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int OnTestKeyDown(InterfaceNode* self, nint context, nuint wParam, nint lParam, int* eaten) =>
        TsfUnmanagedBoundary.Invoke(() => TestKey(self, wParam, eaten));

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int OnTestKeyUp(InterfaceNode* self, nint context, nuint wParam, nint lParam, int* eaten) =>
        TsfUnmanagedBoundary.Invoke(() => SetEaten(self, eaten, false));

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int OnKeyDown(InterfaceNode* self, nint context, nuint wParam, nint lParam, int* eaten) =>
        TsfUnmanagedBoundary.Invoke(() => HandleKeyDown(self, context, wParam, eaten));

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int OnKeyUp(InterfaceNode* self, nint context, nuint wParam, nint lParam, int* eaten) =>
        TsfUnmanagedBoundary.Invoke(() => SetEaten(self, eaten, false));

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int OnPreservedKey(InterfaceNode* self, nint context, Guid* keyId, int* eaten) =>
        TsfUnmanagedBoundary.Invoke(() => SetEaten(self, eaten, false));

    private static int ValidateActive(InterfaceNode* self)
    {
        if (self is null)
        {
            return TsfHResults.E_POINTER;
        }

        var owner = self->Owner;
        return owner->ApartmentThreadId == TsfApartment.CurrentManagedThreadId && owner->IsActive != 0
            ? TsfHResults.S_OK
            : TsfHResults.E_UNEXPECTED;
    }

    private static int TestKey(InterfaceNode* self, nuint wParam, int* eaten)
    {
        var hr = SetEaten(self, eaten, IsSupportedKey(wParam));
        return hr;
    }

    private static int SetEaten(InterfaceNode* self, int* eaten, bool value)
    {
        if (eaten is null)
        {
            return TsfHResults.E_POINTER;
        }

        *eaten = 0;
        var hr = ValidateActive(self);
        if (hr >= 0)
        {
            *eaten = value ? 1 : 0;
        }

        return hr;
    }

    private static bool IsSupportedKey(nuint wParam) =>
        wParam is >= 0x30 and <= 0x39 or >= 0x41 and <= 0x5A or 0x08 or 0x0D or 0x20;

    private static int ActivateCore(InterfaceNode* self, nint threadManager, uint clientId)
    {
        if (self is null || threadManager == 0)
        {
            return TsfHResults.E_POINTER;
        }

        var owner = self->Owner;
        if (owner->ApartmentThreadId != TsfApartment.CurrentManagedThreadId || owner->IsActive != 0)
        {
            return TsfHResults.E_UNEXPECTED;
        }

        TsfComPointer.AddRef(threadManager);
        owner->ThreadManager = threadManager;
        owner->ClientId = clientId;

        nint keystrokeManager = 0;
        var hr = TsfComPointer.QueryInterface(threadManager, TsfNative.IKeystrokeManagerId, &keystrokeManager);
        if (hr < 0)
        {
            ResetActivation(owner);
            return hr;
        }

        owner->KeystrokeManager = keystrokeManager;
        var vtable = *(void***) keystrokeManager;
        var advise = (delegate* unmanaged[Stdcall]<nint, uint, nint, int, int>) vtable[TsfNative.KeystrokeManagerAdviseKeyEventSinkSlot];
        hr = advise(keystrokeManager, clientId, (nint)(&owner->KeyEventSink), 1);
        if (hr < 0)
        {
            ResetActivation(owner);
            return hr;
        }

        owner->IsKeyEventSinkAdvised = 1;
        owner->IsActive = 1;
        return TsfHResults.S_OK;
    }

    private static int DeactivateCore(TextServiceObject* owner, bool enforceApartment)
    {
        if (owner is null)
        {
            return TsfHResults.E_POINTER;
        }

        if (enforceApartment && owner->ApartmentThreadId != TsfApartment.CurrentManagedThreadId)
        {
            return TsfHResults.E_UNEXPECTED;
        }

        var result = TsfHResults.S_OK;
        if (owner->IsKeyEventSinkAdvised != 0 && owner->KeystrokeManager != 0)
        {
            var vtable = *(void***) owner->KeystrokeManager;
            var unadvise = (delegate* unmanaged[Stdcall]<nint, uint, int>) vtable[TsfNative.KeystrokeManagerUnadviseKeyEventSinkSlot];
            result = unadvise(owner->KeystrokeManager, owner->ClientId);
        }

        ResetActivation(owner);
        return result;
    }

    private static void ResetActivation(TextServiceObject* owner)
    {
        owner->IsActive = 0;
        owner->IsKeyEventSinkAdvised = 0;
        owner->ClientId = 0;
        if (owner->KeystrokeManager != 0)
        {
            TsfComPointer.Release(owner->KeystrokeManager);
            owner->KeystrokeManager = 0;
        }

        if (owner->ThreadManager != 0)
        {
            TsfComPointer.Release(owner->ThreadManager);
            owner->ThreadManager = 0;
        }
    }

    private static int HandleKeyDown(InterfaceNode* self, nint context, nuint wParam, int* eaten)
    {
        var supported = IsSupportedKey(wParam);
        var hr = SetEaten(self, eaten, supported);
        if (hr < 0 || !supported)
        {
            return hr;
        }

        if (context == 0)
        {
            return TsfHResults.E_POINTER;
        }

        return RequestEditSession(context, self->Owner->ClientId);
    }

    internal static int RequestEditSession(nint context, uint clientId)
    {
        if (context == 0)
        {
            return TsfHResults.E_POINTER;
        }

        nint editSession = 0;
        var hr = TsfEditSession.TryCreate(&editSession);
        if (hr < 0)
        {
            return hr;
        }

        try
        {
            var vtable = *(void***) context;
            var requestEditSession = (delegate* unmanaged[Stdcall]<nint, uint, nint, uint, int*, int>)
                vtable[TsfNative.ContextRequestEditSessionSlot];
            var sessionResult = TsfHResults.E_UNEXPECTED;
            hr = requestEditSession(
                context,
                clientId,
                editSession,
                TsfNative.TfEditSessionSync | TsfNative.TfEditSessionReadWrite,
                &sessionResult);
            return hr < 0 ? hr : sessionResult;
        }
        finally
        {
            TsfComPointer.Release(editSession);
        }
    }
}