// See https://aka.ms/new-console-template for more information

using System.Diagnostics;

for (int i = 0; i < int.MaxValue; i++)
{
    var n = Random.Shared.Next();
    Test(n);
}

Console.WriteLine("Hello, World!");


void Test(int n)
{
    byte[] list = new byte[4];
    for (var i = 0; i < list.Length; i++)
    {
        list[i] = GetData(i, n);
    }

    // 顺序和人类看到的是反过来的
    var result = BitConverter.ToInt32(list);
    Debug.Assert(result == n);
}

unsafe byte GetData(int index, int n)
{
    /*
         byte[] bytes = new byte[sizeof(int)];
                Unsafe.As<byte, int>(ref bytes[0]) = value;
                return bytes;
     */
    int* p = &n;
    byte* pByte = (byte*) p;
    return pByte[index];
}