using System;
using System.Collections.Generic;
using System.Reflection.Metadata;

namespace CakifallwurnallDallbereluchar
{
    class Program
    {
        static void Main(string[] args)
        {
            // 从零开始写一个依赖注入容器
            _container = new Container();

            Init();

            Use();
        }

        private static void Init()
        {
            Foo foo = new Foo();
            //_container.Set(typeof(Foo), foo);
            _container.Set(foo);
        }

        private static void Use()
        {
            //Foo foo = (Foo)_container.Get(typeof(Foo));
            Foo foo = _container.Get<Foo>();
            Console.WriteLine(foo.Name);
        }

        private static Container _container;
    }

    public class Foo
    {
        public string Name { set; get; }
    }

    public class Container
    {
        public void Set<T>(T obj)
        {
            Set(typeof(T), obj);
        }

        public void Set(Type type, object obj)
        {
            InnerDictionary[type] = obj;
        }

        public T Get<T>()
        {
            return (T)Get(typeof(T));
        }

        public object Get(Type type)
        {
            return InnerDictionary[type];
        }

        private Dictionary<Type, object> InnerDictionary { get; } = new Dictionary<Type, object>();
    }
}
