using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;

namespace LebaicocaqeregelHojajaycal
{
    class Program
    {
        static void Main(string[] args)
        {
            // 在第一个程序集注入了 F1 代码

            var guid1 = new Guid("{97C70651-EE85-4AED-9E2F-AD73AF34CF5D}");
            var guid2 = new Guid("{05D1936F-7121-43BA-B986-A42A56555AAE}");

            var f1 = new F1();
            DynamicProxy.Add(guid1, f1);
            DynamicProxy.Add(guid2, f1);

            // 自己实现两个接口
            var f2 = DynamicProxy.GetObject<IF2>(guid1);
            Console.WriteLine(f2.GetName());
            Console.WriteLine(f2.GetName(new F3()));

            var f3 = DynamicProxy.GetObject<IF3>(guid2);
            Console.WriteLine(f3.GetName());
        }
    }

    class DynamicProxy
    {
        public static void Add(Guid guid, object instance)
        {
            _dictionary[guid] = new Express(new Lazy<object>(() => instance));
        }

        public static T GetObject<T>(Guid guid)
        {
            if (!typeof(T).IsInterface)
            {
                throw new ArgumentException();
            }

            return (T) new Proxy(typeof(T), _dictionary[guid]).GetTransparentProxy();
        }

        // 实际代码请使用缓存池
        private static Dictionary<Guid, Express> _dictionary = new Dictionary<Guid, Express>();

        class Express
        {
            /// <inheritdoc />
            public Express(Lazy<object> instance)
            {
                _instance = instance;
            }

            public object Instance => _instance.Value;

            private readonly Lazy<object> _instance;
        }

        class Proxy : RealProxy
        {
            /// <inheritdoc />
            public Proxy(Type classToProxy, Express express) : base(classToProxy)
            {
                Express = express;
            }

            public Express Express { get; }

            /// <inheritdoc />
            public override IMessage Invoke(IMessage msg)
            {
                MethodCallMessageWrapper callMessageWrapper = new MethodCallMessageWrapper((IMethodCallMessage) msg);
                MethodInfo methodBase = callMessageWrapper.MethodBase as MethodInfo;
                if (methodBase == null)
                    return null;

                var instance = Express.Instance;
                var type = instance.GetType();

                Type[] argumentTypeList;
                if (callMessageWrapper.Args?.Any() is true)
                {
                    argumentTypeList = callMessageWrapper.Args.Select(temp => temp.GetType()).ToArray();
                }
                else
                {
                    argumentTypeList = Type.EmptyTypes;
                }

                var method = type.GetMethod(methodBase.Name, argumentTypeList);

                if (method == null)
                {
                    throw new ArgumentException("调用方法不匹配，找不到" + methodBase + "方法");
                }

                return new ReturnMessage(
                    method.Invoke(instance, callMessageWrapper.Args),
                    callMessageWrapper.Args, callMessageWrapper.ArgCount, callMessageWrapper.LogicalCallContext,
                    callMessageWrapper);
            }
        }
    }

    class F3 : IF3
    {
        /// <inheritdoc />
        public string GetName()
        {
            return null;
        }
    }

    interface IF3
    {
        string GetName();
    }

    interface IF2
    {
        string GetName();
        string GetName(IF3 f3);
    }

    class F1
    {
        public string GetName()
        {
            return "林德熙是逗比";
        }

        public string GetName(IF3 f3)
        {
            return "林德熙是逗比";
        }
    }
}