using System;

namespace YadernawcoLofeleabe
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        }
    }

    public class Example
    {
    	public static string Hello(string yourName)
    	{
    		return $"Hello {yourName}";
    	}
    }
}


/*

csc /target:library -out:Example.dll /noconfig /nostdlib /r:f:/lindexi/mono/wasm-bcl/wasm/mscorlib.dll /r:f:/lindexi/mono/wasm-bcl/wasm/System.dll /r:f:/lindexi/mono/wasm-bcl/wasm/System.Core.dll /r:f:/lindexi/mono/wasm-bcl/wasm/Facades/netstandard.dll /r:f:/lindexi/mono/wasm-bcl/wasm/System.Net.Http.dll /r:f:/lindexi/mono/framework/WebAssembly.Bindings.dll /r:f:/lindexi/mono/framework/WebAssembly.Net.Http.dll Program.cs

"c:\Program Files\Mono\bin\mono" f:/lindexi/mono/packager.exe --copy=always --out=./publish --asset=./index.html Example.dll

*/