// See https://aka.ms/new-console-template for more information

using System.Runtime.InteropServices;

unsafe
{
    Console.WriteLine($"sizeof(TextFontVariant1)={sizeof(TextFontVariant1)}");
    Console.WriteLine($"sizeof(TextFontVariants1)={sizeof(TextFontVariants1)}");

    Console.WriteLine($"sizeof(TextFontVariant2)={sizeof(TextFontVariant2)}");
    Console.WriteLine($"sizeof(TextFontVariants2)={sizeof(TextFontVariants2)}");

    Console.WriteLine($"sizeof(Run1)={SizeOf<Run1>()}");
    Console.WriteLine($"sizeof(Run2)={SizeOf<Run2>()}");

    /*
       sizeof(TextFontVariant1)=8
       sizeof(TextFontVariants1)=4
       sizeof(TextFontVariant2)=2
       sizeof(TextFontVariants2)=1
       sizeof(Run1)=40
       sizeof(Run2)=24

       对于 Run1 来说，每个 TextFontVariant1 是 8 个byte长度，总长度=3*sizeof(TextFontVariant1)+Header(16)=3*8+16=40
       对于 Run2 来说，每个 TextFontVariant2 是 2 个byte长度，总长度=3*sizeof(TextFontVariant2)+Header(16)=3*2+16=24
    */
}

static unsafe Int32 SizeOf<T>()
{
    return ((MethodTableInfo*) (typeof(T).TypeHandle.Value.ToPointer()))->Size;
}

[StructLayout(LayoutKind.Explicit)]
public unsafe struct MethodTableInfo
{
    #region Basic Type Info

    [FieldOffset(0)]
    public int Flags;

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
    public int* ModuleInfo;

    [FieldOffset(24)]
    public int* EEClass;
}

record Run1(TextFontVariant1 F1, TextFontVariant1 F2, TextFontVariant1 F3);
record Run2(TextFontVariant2 F1, TextFontVariant2 F2, TextFontVariant2 F3);

readonly record struct TextFontVariant1()
{
    public bool IsNormal { get; init; }

    public TextFontVariants1 FontVariants { get; init; }
}

enum TextFontVariants1
{
    /// <summary>
    /// 正常，非上下标
    /// </summary>
    Normal = 0,

    /// <summary>
    /// 上标
    /// </summary>
    Superscript,

    /// <summary>
    /// 下标
    /// </summary>
    Subscript,
}

readonly record struct TextFontVariant2()
{
    public bool IsNormal { get; init; }

    public TextFontVariants2 FontVariants { get; init; }
}

enum TextFontVariants2 : byte
{
    /// <summary>
    /// 正常，非上下标
    /// </summary>
    Normal = 0,

    /// <summary>
    /// 上标
    /// </summary>
    Superscript,

    /// <summary>
    /// 下标
    /// </summary>
    Subscript,
}