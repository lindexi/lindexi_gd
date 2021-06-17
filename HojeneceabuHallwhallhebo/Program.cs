using System;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace HojeneceabuHallwhallhebo
{
    class Program
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            var program = new Program();
            program.F2();

            GC.Collect();
            GC.WaitForFullGCComplete();
            GC.Collect();

            Task.Delay(1000).Wait();

            Console.WriteLine("Hello World!");
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
        }

        private void F1()
        {
            Foo foo = null;
            try
            {
                foo = new Foo();
            }
            catch
            {
                // 忽略
            }
            finally
            {
                try
                {
                    foo?.Dispose();
                }
                catch 
                {
                   // 刚好 foo 对象是空，因此不会进入此函数
                }
            }
        }

        private void F2()
        {
            Foo foo = null;
            try
            {
                foo = (Foo) FormatterServices.GetUninitializedObject(typeof(Foo));
                var constructorInfo = typeof(Foo).GetConstructor(new Type[0]);
                constructorInfo!.Invoke(foo, null);
            }
            catch
            {
                // 忽略
            }
            finally
            {
                try
                {
                    foo?.Dispose();
                }
                catch
                {
                    // 可以调用到 Dispose 方法
                }
            }
        }
    }

    class Foo : IDisposable
    {
        public Foo()
        {
            throw new Exception("lindexi is doubi");
        }

        ~Foo()
        {
            Dispose();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);

            throw new Exception($"lsj is doubi");
        }
    }
}