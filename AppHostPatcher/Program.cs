// copy from https://github.com/dnSpy/dnSpy/blob/2fa5c978b1a9fb8d1979c8aa4cfa6d177bf5aa9c/Build/AppHostPatcher/Program.cs

using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace AppHostPatcher
{
	/// <summary>
	/// 在 dotnet runtime\src\installer\corehost\ 将会构建出 apphost.exe 文件
	/// 在安装 dotnet sdk 的时候，将会输出到 c:\Program Files\dotnet\sdk\5.0.100\AppHostTemplate\apphost.exe 文件夹
	/// 这个 apphost.exe 文件将会在构建的时候，输出到 obj 文件夹里面
	/// 然后被替换执行的 dll 路径，根据 dotnet runtime\src\installer\corehost\corehost.cpp 的注释以及 https://github.com/dnSpy/dnSpy/blob/2fa5c978b1a9fb8d1979c8aa4cfa6d177bf5aa9c/Build/AppHostPatcher/Program.cs 的代码，可以了解到，替换这个路径就可以自己定制执行的路径
	/// </summary>
	class Program
	{
		static void Usage()
		{
			Console.WriteLine("apphostpatcher.exe <apphostexe> <newdllpath>");
			Console.WriteLine("使用方法: apphostpatcher Foo.exe C:\\新的文件夹\\Foo.dll");
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

		static int Main(string[] args)
		{
			try
			{
				string apphostExe, origPath, newPath;
				if (args.Length == 2)
				{
					apphostExe = args[0];
					origPath = Path.GetFileName(ChangeExecutableExtension(apphostExe));
					newPath = args[1];
				}
				else
				{
					Usage();
					return 1;
				}

				if (!File.Exists(apphostExe))
				{
					Console.WriteLine($"Apphost '{apphostExe}' does not exist");
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

				var apphostExeBytes = File.ReadAllBytes(apphostExe);
				int offset = GetOffset(apphostExeBytes, origPathBytes);
				if (offset < 0)
				{
					Console.WriteLine($"Could not find original path '{origPath}'");
					return 1;
				}
				if (offset + newPathBytes.Length > apphostExeBytes.Length)
				{
					Console.WriteLine($"New path is too long: {newPath}");
					return 1;
				}
				for (int i = 0; i < newPathBytes.Length; i++)
					apphostExeBytes[offset + i] = newPathBytes[i];
				File.WriteAllBytes(apphostExe, apphostExeBytes);
				return 0;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
				return 1;
			}
		}

		static int GetOffset(byte[] bytes, byte[] pattern)
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

		static bool Match(byte[] bytes, int index, byte[] pattern)
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
