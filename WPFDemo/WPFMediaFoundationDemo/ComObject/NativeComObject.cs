using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Threading;
using Windows.Win32.System.Com;

namespace ComSamples;

internal sealed unsafe class NativeComObject : IDisposable
{
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int Query(nint self, nint iid, nint result);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate uint Ref(nint self);

    private readonly Delegate[] callbacks;
    private readonly nint table;
    private nint instance;
    private int references = 1;
    private GCHandle root;

    internal NativeComObject(bool dynamicDelegates)
    {
        callbacks = new Delegate[] { new Query(QueryInterface), new Ref(AddRef), new Ref(Release) };
        if (dynamicDelegates)
        {
            for (int i = 0; i < callbacks.Length; i++)
                callbacks[i] = CreateThunk(callbacks[i]);
        }
        table = Marshal.AllocHGlobal(3 * nint.Size);
        for (int i = 0; i < callbacks.Length; i++)
            Marshal.WriteIntPtr(table, i * nint.Size, Marshal.GetFunctionPointerForDelegate(callbacks[i]));
        instance = Marshal.AllocHGlobal(nint.Size);
        Marshal.WriteIntPtr(instance, table);
        root = GCHandle.Alloc(this);
    }

    internal IUnknown* Pointer => (IUnknown*)instance;

    private int QueryInterface(nint self, nint iid, nint result)
    {
        if (result == 0) return unchecked((int)0x80004003);
        Marshal.WriteIntPtr(result, 0);
        if (iid == 0) return unchecked((int)0x80004003);
        if (Marshal.PtrToStructure<Guid>(iid) != typeof(IUnknown).GUID)
            return unchecked((int)0x80004002);
        AddRef(self);
        Marshal.WriteIntPtr(result, self);
        return 0;
    }

    private uint AddRef(nint self) => (uint)Interlocked.Increment(ref references);

    private uint Release(nint self)
    {
        int remaining = Interlocked.Decrement(ref references);
        if (remaining == 0)
        {
            Marshal.FreeHGlobal(instance);
            instance = 0;
            // The vtable is owned by Dispose, after the final callback has returned.
            root.Free();
        }
        return (uint)remaining;
    }

    public void Dispose()
    {
        if (instance != 0) throw new InvalidOperationException("COM references remain outstanding.");
        Marshal.FreeHGlobal(table);
        GC.KeepAlive(callbacks);
    }

    private static Delegate CreateThunk(Delegate target)
    {
        var invoke = target.GetType().GetMethod("Invoke") ?? throw new MissingMethodException();
        var parameters = Array.ConvertAll(invoke.GetParameters(), p => Expression.Parameter(p.ParameterType, p.Name));
        var module = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("ComThunk" + Guid.NewGuid().ToString("N")), AssemblyBuilderAccess.RunAndCollect).DefineDynamicModule("Thunks");
        var type = module.DefineType("NativeCallback", TypeAttributes.Public | TypeAttributes.Sealed, typeof(MulticastDelegate));
        var attribute = typeof(UnmanagedFunctionPointerAttribute).GetConstructor(new[] { typeof(CallingConvention) }) ?? throw new MissingMethodException();
        type.SetCustomAttribute(new CustomAttributeBuilder(attribute, new object[] { CallingConvention.StdCall }));
        type.DefineConstructor(MethodAttributes.Public | MethodAttributes.RTSpecialName | MethodAttributes.HideBySig, CallingConventions.Standard, new[] { typeof(object), typeof(nint) }).SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);
        type.DefineMethod("Invoke", MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot, invoke.ReturnType, Array.ConvertAll(parameters, p => p.Type)).SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);
        return Expression.Lambda(type.CreateType()!, Expression.Invoke(Expression.Constant(target), parameters), parameters).Compile();
    }

    internal static void Verify(bool dynamicDelegates)
    {
        using var owner = new NativeComObject(dynamicDelegates);
        var pointer = owner.Pointer;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        Guid iid = typeof(IUnknown).GUID;
        void* queried = null;
        pointer->QueryInterface(&iid, &queried).ThrowOnFailure();
        if (queried != pointer) throw new InvalidOperationException("IUnknown identity mismatch.");
        if (((IUnknown*)queried)->Release() != 1) throw new InvalidOperationException("QueryInterface reference mismatch.");
        iid = Guid.NewGuid();
        queried = null;
        var unsupported = pointer->QueryInterface(&iid, &queried);
        if (unsupported.Value != unchecked((int)0x80004002) || queried != null) throw new InvalidOperationException("Unsupported interface was accepted.");
        if (pointer->AddRef() != 2 || pointer->Release() != 1 || pointer->Release() != 0)
            throw new InvalidOperationException("COM reference count mismatch.");
        Console.WriteLine($"PASS COM dynamic={dynamicDelegates}, architecture={RuntimeInformation.ProcessArchitecture}");
    }
}
