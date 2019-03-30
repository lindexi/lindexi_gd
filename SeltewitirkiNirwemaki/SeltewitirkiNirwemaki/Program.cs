using System;
using System.IO;
using System.Runtime.InteropServices;

namespace SeltewitirkiNirwemaki
{
    class Program
    {
        static void Main(string[] args)
        {
            var folder = @"D:\lindexi\github\";
            var file = Path.Combine(folder, "欢迎访问我博客 blog.lindexi.com 里面有大量 UWP WPF 博客.txt");
            Directory.CreateDirectory(folder);
            if (!File.Exists(file))
            {
                File.WriteAllText(file, "欢迎访问我博客 blog.lindexi.com 里面有大量 UWP WPF 博客");
            }

            RecycleBin.DeleteToRecycleBin(file);
        }
    }

    public static class RecycleBin
    {
        public static void DeleteToRecycleBin(string file)
        {
            var shf = new SHFILEOPSTRUCT
            {
                wFunc = FO_DELETE,
                fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION,
                // pFrom 需要在字符串后面加两个 \0 才可以 https://docs.microsoft.com/en-us/windows/desktop/api/shellapi/ns-shellapi-_shfileopstructa
                pFrom = file + "\0"
            };
            SHFileOperation(ref shf);
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 1)]
        private struct SHFILEOPSTRUCT
        {
            public int hwnd;
            [MarshalAs(UnmanagedType.U4)] public int wFunc;
            public string pFrom;
            public string pTo;
            public short fFlags;
            [MarshalAs(UnmanagedType.Bool)] public bool fAnyOperationsAborted;
            public int hNameMappings;
            public string lpszProgressTitle;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern int SHFileOperation(ref SHFILEOPSTRUCT FileOp);

        private const int FO_DELETE = 3;
        private const int FOF_ALLOWUNDO = 0x40;
        private const int FOF_NOCONFIRMATION = 0x10;
    }
}