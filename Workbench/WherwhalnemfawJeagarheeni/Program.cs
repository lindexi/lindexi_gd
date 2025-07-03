// See https://aka.ms/new-console-template for more information
var foo = DoFoo(5);
Console.WriteLine(foo);


static Foo DoFoo(int n)
{
    var foo = new Foo();
    var t1 = foo;
    /*
       var t1 = foo;
       00007FFAD11B5482  vxorps      ymm0,ymm0,ymm0  
       00007FFAD11B5486  vmovdqu     ymmword ptr [rbp-50h],ymm0  
       00007FFAD11B548B  vmovdqu     xmmword ptr [rbp-38h],xmm0  
       00007FFAD11B5490  vmovdqu     ymm0,ymmword ptr [rbp-50h]  
       00007FFAD11B5495  vmovdqu     ymmword ptr [rbp-28h],ymm0  
       00007FFAD11B549A  mov         rax,qword ptr [rbp-30h]  
       00007FFAD11B549E  mov         qword ptr [rbp-8],rax

       这段汇编代码是使用 AVX 指令集进行向量操作和内存读写的典型例子。下面是每条指令的含义逐条解释：
       
       ---
       
       ### 🧠 指令解析
       
       1. **`vxorps ymm0, ymm0, ymm0`**
          - 将 `ymm0` 寄存器清零（即所有位设为 0）。
          - 这是通过将寄存器自身与自身按位异或实现的高效“置零”方式。
       
       2. **`vmovdqu ymmword ptr [rbp-50h], ymm0`**
          - 将 `ymm0` 中的 256 位（32 字节）数据存储到栈帧中偏移 `-0x50` 的位置。
          - `vmovdqu` 表示“移动未对齐的双字节向量”，适用于任意内存对齐。
       
       3. **`vmovdqu xmmword ptr [rbp-38h], xmm0`**
          - 将 `xmm0`（`ymm0` 的低 128 位）存储到偏移 `-0x38` 的位置。
          - 说明此处只需要保存一半的数据。
       
       4. **`vmovdqu ymm0, ymmword ptr [rbp-50h]`**
          - 从偏移 `-0x50` 的内存位置读取 256 位数据到 `ymm0`。
       
       5. **`vmovdqu ymmword ptr [rbp-28h], ymm0`**
          - 将刚刚读取的 `ymm0` 数据再次写入到偏移 `-0x28` 的位置。
       
       6. **`mov rax, qword ptr [rbp-30h]`**
          - 从偏移 `-0x30` 的位置读取 64 位数据到 `rax` 寄存器。
       
       7. **`mov qword ptr [rbp-8], rax`**
          - 将 `rax` 中的数据写入偏移 `-0x8` 的位置。
       
       ---
       
       ### 🧩 总结
       
       这段代码的主要作用是：
       - 初始化 `ymm0` 为全零；
       - 将其保存到栈中多个位置；
       - 进行一次读取和再次写入；
       - 同时处理一个普通的 64 位数据搬运。
       
       看起来像是编译器自动生成的初始化代码，可能用于清空结构体或数组的内存区域。
     */
    t1.N1 = n;
    return t1;
}

struct Foo
{
    public int N1 { get; set; } 
    public int N2 { get; set; } 
    public int N3 { get; set; } 
    public int N4 { get; set; } 
    public int N5 { get; set; } 
    public int N6 { get; set; } 
    public int N7 { get; set; } 
    public int N8 { get; set; } 
    public int N9 { get; set; } 
    public int N10 { get; set; }
}