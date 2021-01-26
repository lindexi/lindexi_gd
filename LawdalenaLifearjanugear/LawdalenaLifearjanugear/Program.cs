using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;

namespace LawdalenaLifearjanugear
{
    class Program
    {
        static void Main(string[] args)
        {
            var memoryStream = new MemoryStream();
            var binaryWriter = new BinaryWriter(memoryStream);
            for (int i = 0; i < 100; i++)
            {
                binaryWriter.Write(i);
            }

            memoryStream.Position = 0;

            var byteList = memoryStream.ToArray();

            unsafe
            {
                var length = byteList.Length / sizeof(int);
                fixed (byte* bytePointer = byteList)
                {
                    int* intList = (int*)bytePointer;
                    for (int i = 0; i < length; i++)
                    {
                        int value = *intList;
                        Console.WriteLine(value);
                        intList++;
                    }
                }
            }

            memoryStream.Position = 0;


            for (int i = 0; i < 100; i++)
            {
                //var fooStruct = new FooStruct()
                //{
                //    N1 = i,
                //    N2 = i,
                //    N3 = i
                //};
                binaryWriter.Write(i);
                binaryWriter.Write(i);
                binaryWriter.Write(i);
            }

            memoryStream.Position = 0;

            byteList = memoryStream.ToArray();

            unsafe
            {
                var length = byteList.Length / sizeof(FooStruct);
                fixed (byte* bytePointer = byteList)
                {
                    var fooStructList = (FooStruct*)bytePointer;
                    for (int i = 0; i < length; i++)
                    {
                        var fooStruct = *fooStructList;

                        Console.WriteLine(fooStruct.N1);
                        Console.WriteLine(fooStruct.N2);
                        Console.WriteLine(fooStruct.N3);
                        fooStructList++;
                    }
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        struct FooStruct
        {
            public int N1 { get; set; }
            public int N2 { get; set; }
            public int N3 { get; set; }
        }
    }
}
