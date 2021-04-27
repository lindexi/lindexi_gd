using System;
using System.Runtime.InteropServices;

namespace HaynogelwhaiFaycemferlerluja
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(SizeOf<Program>());
        }

        // 代码 [sidristij/dotnetex: Gets size of .Net Framework objects, can change type of object to incompatible and can alloc .Net objects at unmanaged memory area](https://github.com/sidristij/dotnetex )

        private int F1 { get; }
        private int F2 { get; }
        private int F3 { get; }

        /// <summary>
        /// Gets type size in unmanaged memory (directly in SOH/LOH) by type
        /// </summary>
        public static unsafe Int32 SizeOf<T>()
        {
            return ((MethodTableInfo*)(typeof(T).TypeHandle.Value.ToPointer()))->Size;
        }

        /// <summary>
        /// Description of GCEnumerator.
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        public unsafe struct MethodTableInfo
        {
            #region Basic Type Info

            [FieldOffset(0)]
            public MethodTableFlags Flags;

            [FieldOffset(4)]
            public int Size;

            [FieldOffset(8)]
            public short AdditionalFlags;

            [FieldOffset(10)]
            public short MethodsCount;

            [FieldOffset(12)]
            public short VirtMethodsCount;

            [FieldOffset(14)]
            public short InterfacesCount;

            [FieldOffset(16)]
            public MethodTableInfo* ParentTable;

            #endregion

            [FieldOffset(20)]
            public ObjectTypeInfo* ModuleInfo;

            [FieldOffset(24)]
            public ObjectTypeInfo* EEClass;
        }

        [StructLayout(LayoutKind.Explicit)]
        public unsafe struct ObjectTypeInfo
        {
            [FieldOffset(0)]
            public ObjectTypeInfo* ParentClass;

            [FieldOffset(16)]
            public MethodTableInfo* MethodsTable;
        }

        // ReSharper disable InconsistentNaming
        [Flags]
        public enum MethodTableFlags : uint
        {
            Array = 0x00010000,

            InternalCorElementTypeExtraInfoMask = 0x00060000,
            InternalCorElementTypeExtraInfo_IfNotArrayThenTruePrimitive = 0x00020000,
            InternalCorElementTypeExtraInfo_IfNotArrayThenClass = 0x00040000,
            InternalCorElementTypeExtraInfo_IfNotArrayThenValueType = 0x00060000,

            IfArrayThenMultidim = 0x00020000,
            IfArrayThenSharedByReferenceTypes = 0x00040000,

            ContainsPointers = 0x00080000,
            HasFinalizer = 0x00100000, // instances require finalization

            IsMarshalable = 0x00200000, // Is this type marshalable by the pinvoke marshalling layer

            HasRemotingVtsInfo = 0x00400000, // Optional data present indicating VTS methods and optional fields
            IsFreezingRequired = 0x00800000, // Static data should be frozen after .cctor

            TransparentProxy = 0x01000000, // tranparent proxy
            CompiledDomainNeutral = 0x02000000, // Class was compiled in a domain neutral assembly

            // This one indicates that the fields of the valuetype are 
            // not tightly packed and is used to check whether we can
            // do bit-equality on value types to implement ValueType::Equals.
            // It is not valid for classes, and only matters if ContainsPointer
            // is false.
            //
            NotTightlyPacked = 0x04000000,

            HasCriticalFinalizer = 0x08000000, // finalizer must be run on Appdomain Unload
            UNUSED = 0x10000000,
            ThreadContextStatic = 0x20000000,

            IsFreezingCompleted = 0x80000000, // Static data has been frozen

            NonTrivialInterfaceCast = Array | TransparentProxy,
        }
    }
}
