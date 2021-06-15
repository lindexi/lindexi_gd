using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace GewelhalllemniGafihaja
{
    class Program
    {
        static void Main(string[] args)
        {
            var str = "Hello";

            GC.TryStartNoGCRegion(1000000);
            unsafe
            {
                fixed (char* sp = str)
                {
                    var a = new ByValStringStructForSizeMAX_PATH();
                    Buffer.MemoryCopy(sp, &a, 260 * 2, 5 * 2);
              
                    Console.WriteLine(a);
                }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Size = 260 * sizeof(char))]
    public unsafe struct ByValStringStructForSizeMAX_PATH
    {
        char _firstChar;
        //char _foo;

        /// <inheritdoc/>
        public override string ToString()
        {
            fixed (char* charPtr = &_firstChar)
            {
                //var t = charPtr;
                //StringBuilder str = new StringBuilder();
                //int i = 0;
                //while (true)
                //{
                //    char c = *t;
                //    t++;

                //    if (c == 0)
                //    {
                //        break;
                //    }

                //    i++;

                //    str.Append(c);
                //}

                //str.Append(" ").Append(i).Append(";").Append(GetHashCode()).Append(";").Append(_firstChar.GetHashCode());

                //fixed (void* p = &this)
                //{
                //    str.Append(";").Append(((IntPtr) (&p)));
                //}

                //return str.ToString();

                return new string(charPtr);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="val"></param>
        public static implicit operator string(ByValStringStructForSizeMAX_PATH val) => val.ToString();
    }
}
