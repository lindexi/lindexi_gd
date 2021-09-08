using System;
using System.Runtime.CompilerServices;

namespace KicheyurcherNiwhiyuhawnelkeera
{
    class Program
    {
        static void Main(string[] args)
        {
            Program program = new();
            program.F2();
        }

        private void F2()
        {
            Foo foo = null;
            try
            {
                foo = (Foo)RuntimeHelpers.GetUninitializedObject(typeof(Foo));
                foo.F1 = 2;
                foo.F2 = 2;
                var constructorInfo = typeof(Foo).GetConstructor(new Type[0]);
                constructorInfo!.Invoke(foo, null);

                foo.F1 = 5;
                foo.F2 = 5;
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

        class Foo : IDisposable
        {
            public Foo()
            {
            }

            public int F1 { set; get; }
            public int F2 { set; get; } = 10;

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
}
