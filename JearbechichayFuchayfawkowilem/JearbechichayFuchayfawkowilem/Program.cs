using System;

namespace JearbechichayFuchayfawkowilem
{
    class Program
    {
        static void Main(string[] args)
        {
            var fooHandle = new FooHandler();
            var f1Filter = new F1Filter();
            using (f1Filter)
            {
                fooHandle.AddF1Filter(f1Filter);
                // 其他业务
            }
            HandleFoo(fooHandle);
        }

        private static void HandleFoo(IFooHandler fooHandler)
        {
            fooHandler.AddF1Filter(new F1Filter());
            fooHandler.AddF1Filter(new F1Filter());
            fooHandler.AddF1Filter(new F1Filter());
        }
    }

    class FooHandler : IFooHandler
    {
        public void AddF1Filter(IF1Filter filter)
        {
        }

        public void RemoveF1Filter(IF1Filter filter)
        {
            
        }

        public void AddF2Filter(IF2Filter filter)
        {
        }

        public void SetF1Filter(IF1Filter filter)
        {
            Filter = filter;
        }

        private IF1Filter Filter { get; set; } = DefaultFilter;

        private static readonly IF1Filter DefaultFilter = new F1Filter();
    }


    interface IFooHandler
    {
        void AddF1Filter(IF1Filter filter);
        void RemoveF1Filter(IF1Filter filter);
        void AddF2Filter(IF2Filter filter);
    }

    interface IF2Filter
    {
    }

    class F1Filter : IF1Filter, IDisposable
    {
        public void Dispose()
        {
            FooHandler.RemoveF1Filter(this);
        }

        public IFooHandler FooHandler { get; set; }
    }

    interface IF1Filter
    {
        IFooHandler FooHandler { set; get; }
    }
}