using System;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;

namespace BaljuhairhearkelfereKakikunall
{
    class Program
    {
        static void Main(string[] args)
        {
            var obj = new TestObject();
            obj.Foo += Obj_Foo;

            Console.Read();
        }

        private static void Obj_Foo(object sender, int e)
        {
        }
    }

    class TestObject
    {
        public event EventHandler<int> Foo;

        //调用下面方法将会触发 Foo 事件
        protected virtual void OnFoo(int e)
        {
            Foo?.Invoke(this, e);
        }
    }
}
