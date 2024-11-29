// See https://aka.ms/new-console-template for more information
using System.Runtime.InteropServices;
unsafe
{
    var name = stackalloc byte[1024];
    var result = gethostname(name, 1024);

    var t = Marshal.PtrToStringAnsi(new IntPtr(name));

    Console.WriteLine($"name={t};result={result}");

    /*
       这个函数需要两个参数：

       接收缓冲区name，其长度必须为len字节或是更长,存获得的主机名。

       接收缓冲区name的最大长度

       返回值：

       如果函数成功，则返回0。如果发生错误则返回-1。错误号存放在外部变量errno中
     */
    [DllImport("libc")]
    static extern int gethostname(byte* name, int len);
}
