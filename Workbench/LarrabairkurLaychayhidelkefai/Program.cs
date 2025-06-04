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
    Console.WriteLine($"sizeof(Run3)={SizeOf<Run3>()}");

    /*
       sizeof(TextFontVariant1)=8
       sizeof(TextFontVariants1)=4
       sizeof(TextFontVariant2)=2
       sizeof(TextFontVariants2)=1
       sizeof(Run1)=40
       sizeof(Run2)=24

       对于 Run1 来说，每个 TextFontVariant1 是 8 个byte长度，总长度=3*sizeof(TextFontVariant1)+Header&Pad(16)=3*8+16=40
       对于 Run2 来说，每个 TextFontVariant2 是 2 个byte长度，总长度=3*sizeof(TextFontVariant2)+Header&Pad(16)=3*2+16=6+16，将6对齐到8，结果为 8+16=24
       对于 Run3 来说，尽管比 Run2 多了一个 TextFontVariant2 字段，但刚好还在对齐范围内 4*sizeof(TextFontVariant2)+Header&Pad(16)=8+Header&Pad(16)=8+16=24
    */

    Console.WriteLine($"sizeof(Foo1)={sizeof(Foo1)}");
    Console.WriteLine($"sizeof(Foo2)={SizeOf<Foo2>()}");
    Console.WriteLine($"sizeof(Foo3)={SizeOf<Foo3>()}");

    Console.WriteLine($"sizeof(TextFontVariant)={sizeof(TextFontVariant)}"); // sizeof(TextFontVariant)=16
    // 无论用 byte 还是 int 都是 16 长度
}

static unsafe Int32 SizeOf<T>()
{
    return ((MethodTableInfo*) (typeof(T).TypeHandle.Value.ToPointer()))->Size;
}

readonly record struct Foo1()
{
    public bool F1 { get; init; }
    public bool F2 { get; init; }
    public bool F3 { get; init; }
}

record Foo2()
{
    public Foo1 F1 { get; set; }
}

record Foo3()
{
    public Foo1 F1 { get; set; }
    public Foo1 F2 { get; set; }
    public bool F3 { get; set; }
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
record Run3(TextFontVariant2 F1, TextFontVariant2 F2, TextFontVariant2 F3, TextFontVariant2 Foo);

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

/// <summary>
/// 表示文本上下标字符属性
/// </summary>
/// 在 PPT 里面，使用 Baseline 表示上下标的距离。使用正数表示上标，使用负数表示下标，详细请看 059b7e5807c33ebc6e9156971e6b8d51235a9c0e
/// 在 Word 里面，采用 VerticalAlignment Value= "Subscript" 或 "Superscript" 来表示下标或上标
/// 采用 PPT 的定义方式，可以包含 Word 的功能。但 PPT 里面使用正数负数表示比较不直观，且判断逻辑稍微复杂，不如再添加一个枚举属性好了。加一个枚举只加一个 sizeof(byte) 长度，不亏
public readonly record struct TextFontVariant()
{
    /// <summary>
    /// 正常，非上下标
    /// </summary>
    //public bool IsNormal => FontVariants == TextFontVariants1.Normal;

    /// <summary>
    /// 上标或下标的基线比例
    /// </summary>
    /// 和 PPT 不同的是，不采用正负号来表示。需要配合表示上下标的属性来表示
    public double BaselineProportion { get; init; } = 0.3;

    /// <summary>
    /// 上下标
    /// </summary>
    TextFontVariants FontVariants { get; init; } = TextFontVariants.Normal;

    /// <summary>
    /// 正常，非上下标
    /// </summary>
    public static TextFontVariant Normal => new TextFontVariant();

    /// <summary>
    /// 上标
    /// </summary>
    public static TextFontVariant Superscript => new TextFontVariant()
    {
        //FontVariants = TextFontVariants.Superscript
    };

    /// <summary>
    /// 下标
    /// </summary>
    public static TextFontVariant Subscript => new TextFontVariant()
    {
        //FontVariants = TextFontVariants.Subscript
    };
}

/// <summary>
/// 文本字体变体，上下标
/// </summary>
public enum TextFontVariants : byte
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