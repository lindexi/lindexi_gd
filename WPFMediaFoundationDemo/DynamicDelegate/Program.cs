using Lsj.Util.Reflection;
using Lsj.Util.Win32;
using Lsj.Util.Win32.BaseTypes;
using Lsj.Util.Win32.ComInterfaces;
using Lsj.Util.Win32.Enums;
using Lsj.Util.Win32.Extensions.Memory;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using static Lsj.Util.Win32.Constants;

namespace DynamicDelegate
{
    class Program
    {
        static void Main(string[] args)
        {
            var iUnknown = TestGetIUnknown();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();


            var hResult = iUnknown.Value.QueryInterface(IIDs.IID_IUnknown, out var obj);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var ret = iUnknown.Value.Release();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            ret = iUnknown.Value.AddRef();
        }

        static P<IUnknown> TestGetIUnknown()
        {
            var testObj = new BaseComObject<IUnknown>();
            return testObj.GetNativePointer();
        }


    }

    public unsafe struct VTable<TInterface> where TInterface : struct
    {
        nint* _vTable;
        public VTable(int count)
        {
            _vTable = (nint*)HeapHelper.AllocMemory(count * UnsafePInvokeExtensions.SizeOf<nint>());
        }

        static MethodInfo _getObjectMethod;
        static VTable()
        {
            _getObjectMethod = typeof(NativeComObject<TInterface>).GetMethod(nameof(NativeComObject<TInterface>.GetObj))!;
        }

        public Delegate SetMethod(int offset, MethodInfo method)
        {
            var retType = method.ReturnType;
            var originalParameters = method.GetParameters();


            var parameters = new ParameterExpression[originalParameters.Length + 1];
            parameters[0] = Expression.Parameter(typeof(P<NativeComObject<TInterface>>), "obj");
            for (int i = 0; i < originalParameters.Length; i++)
            {
                parameters[i + 1] = Expression.Parameter(originalParameters[i].ParameterType, originalParameters[i].Name);
            }

            var instance = Expression.Call(_getObjectMethod, parameters[0]);
            var call = Expression.Call(instance, method, parameters.Skip(1));

            // 需要手动创建委托类型，否则可能是 System.Func 或 System.Action，Marshal.GetFunctionPointerForDelegate 会炸掉。
            var lambda = Expression.Lambda(CreateDelegateType($"{method.DeclaringType!.Name}_{method.Name}_thunk_{Guid.NewGuid()}", retType, parameters), call, parameters);
            var @delegate = lambda.Compile();
            _vTable[offset] = Marshal.GetFunctionPointerForDelegate(@delegate);
            return @delegate;
        }

        private Type CreateDelegateType(string name, Type retType, ParameterExpression[] parameters)
        {
            Type[] parameterTypes = parameters.Select(x =>
            {
                if (x.IsByRef)
                {
                    return x.Type.MakeByRefType();
                }
                else
                {
                    return x.Type;
                }
            }).ToArray();

            return DynamicAssemblyHelper.DefineDelegate(name, retType, parameterTypes).CreateTypeInfo();
        }
    }

    internal struct NativeComObject<TInterface> where TInterface : struct
    {
        VTable<TInterface> _vtable;
        GCHandle _objHandle;

        public NativeComObject(VTable<TInterface> vTable, BaseComObject<TInterface> obj)
        {
            _vtable = vTable;
            _objHandle = GCHandle.Alloc(obj, GCHandleType.Normal);
        }

        public void Free()
        {
            _objHandle.Free();
        }

        public static BaseComObject<TInterface> GetObj(P<NativeComObject<TInterface>> nativeComObject)
        {
            return (BaseComObject<TInterface>)nativeComObject.Value._objHandle.Target!;
        }
    }

    public class BaseComObject<TInterface> where TInterface : struct
    {
        P<NativeComObject<TInterface>> _nativeObject;
        uint _refCount = 1;

        public BaseComObject()
        {
            _nativeObject = HeapHelper.AllocMemoryForUnmanagedStruct(new NativeComObject<TInterface>(GetVTable(), this));
        }

        public virtual HRESULT QueryInterface(in IID riid, out IntPtr ppvObject)
        {
            ppvObject = NULL;
            return HRESULT.E_NOTIMPL;
        }

        public virtual ULONG AddRef()
        {
            _refCount++;
            return _refCount;
        }

        public virtual ULONG Release()
        {
            _refCount--;
            if (_refCount == 0)
            {
                _nativeObject.Value.Free();
                HeapHelper.FreeMemory(_nativeObject);
            }
            return _refCount;
        }

        public P<TInterface> GetNativePointer()
        {
            return _nativeObject.AsP<TInterface>();
        }


        static VTable<TInterface>? _vable;
        static Delegate[]? _delegates;

        public VTable<TInterface> GetVTable()
        {
            if (_vable == null)
            {
                _vable = new VTable<TInterface>(3);
                _delegates = new Delegate[3];
                _delegates[0] = _vable.Value.SetMethod(0, GetMethodInfo(nameof(QueryInterface)));
                _delegates[1] = _vable.Value.SetMethod(1, GetMethodInfo(nameof(AddRef)));
                _delegates[2] = _vable.Value.SetMethod(2, GetMethodInfo(nameof(Release)));
            }
            return _vable.Value;
        }

        public static void FreeVTable()
        {
            _vable = null;
            _delegates = null;
        }

        private MethodInfo GetMethodInfo(string method)
        {
            return GetType().GetMethod(method)!;
        }
    }
}