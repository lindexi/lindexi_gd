using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Threading.Tasks;

namespace CakifallwurnallDallbereluchar
{
    class Program
    {
        static void Main(string[] args)
        {
            // 从零开始写一个依赖注入容器
            // 尝试解决线程并发问题
            _container = new Container();

            Init();

            Use();
        }

        private static void Init()
        {
            Foo foo0 = new Foo();
            Foo foo1 = new Foo();
            Foo foo2 = new Foo();
            //_container.Set(typeof(Foo), foo);
            _container.Set(foo0);

            int n = 100;
            var taskList = new List<Task>();
            for (int i = 0; i < n; i++)
            {
                var foo = i < n / 2 ? foo1 : foo2;

                var task = Task.Run(() => _container.Set(foo));
                taskList.Add(task);
            }

            Task.WaitAll(taskList.ToArray());

            var fx = _container.Get<Foo>();
            if (ReferenceEquals(foo0, fx))
            {

            }
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

        private ConcurrentDictionary<Type, object> InnerDictionary { get; } = new ConcurrentDictionary<Type, object>();
    }
}
