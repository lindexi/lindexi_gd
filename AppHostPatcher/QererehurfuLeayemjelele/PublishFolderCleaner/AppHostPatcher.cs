// copy from https://github.com/dnSpy/dnSpy/blob/2fa5c978b1a9fb8d1979c8aa4cfa6d177bf5aa9c/Build/AppHostPatcher/Program.cs

using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace PublishFolderCleaner
{
    class AppHostPatcher
    {
        public static int Patch(string appHostExe, string newPath)
        {
            try
            {
                var origPath = Path.GetFileName(ChangeExecutableExtension(appHostExe));

                if (!File.Exists(appHostExe))
                {
                    Console.WriteLine($"Apphost '{appHostExe}' does not exist");
                    return 1;
                }
                if (origPath == string.Empty)
                {
                    Console.WriteLine("Original path is empty");
                    return 1;
                }
                var origPathBytes = Encoding.UTF8.GetBytes(origPath + "\0");
                Debug.Assert(origPathBytes.Length > 0);
                var newPathBytes = Encoding.UTF8.GetBytes(newPath + "\0");
                if (origPathBytes.Length > MaxPathBytes)
                {
                    Console.WriteLine($"Original path is too long");
                    return 1;
                }
                if (newPathBytes.Length > MaxPathBytes)
                {
                    Console.WriteLine($"New path is too long");
                    return 1;
                }

                var appHostExeBytes = File.ReadAllBytes(appHostExe);
                int offset = GetOffset(appHostExeBytes, origPathBytes);
                if (offset < 0)
                {
                    Console.WriteLine($"Could not find original path '{origPath}'");
                    return 1;
                }
                if (offset + newPathBytes.Length > appHostExeBytes.Length)
                {
                    Console.WriteLine($"New path is too long: {newPath}");
                    return 1;
                }
                for (int i = 0; i < newPathBytes.Length; i++)
                    appHostExeBytes[offset + i] = newPathBytes[i];
                File.WriteAllBytes(appHostExe, appHostExeBytes);
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return 1;
            }
        }

        /// <summary>
        /// 这里有 1024 个 byte 空间用来决定加载路径
        /// 详细请看 dotnet runtime\src\installer\corehost\corehost.cpp 的注释
        /// </summary>
        private const int MaxPathBytes = 1024;

        static string ChangeExecutableExtension(string apphostExe) =>
            // Windows apphosts have an .exe extension. Don't call Path.ChangeExtension() unless it's guaranteed
            // to have an .exe extension, eg. 'some.file' => 'some.file.dll', not 'some.dll'
            apphostExe.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? Path.ChangeExtension(apphostExe, ".dll") : apphostExe + ".dll";

        private static int GetOffset(byte[] bytes, byte[] pattern)
        {
            int si = 0;
            var b = pattern[0];
            while (si < bytes.Length)
            {
                si = Array.IndexOf(bytes, b, si);
                if (si < 0)
                    break;
                if (Match(bytes, si, pattern))
                    return si;
                si++;
            }
            return -1;
        }

        private static bool Match(byte[] bytes, int index, byte[] pattern)
        {
            if (index + pattern.Length > bytes.Length)
                return false;
            for (int i = 0; i < pattern.Length; i++)
            {
                if (bytes[index + i] != pattern[i])
                    return false;
            }
            return true;
        }
    }
}