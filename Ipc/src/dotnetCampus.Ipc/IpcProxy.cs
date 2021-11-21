using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using dotnetCampus.Ipc.Abstractions;
using dotnetCampus.Ipc.Context;
using dotnetCampus.Ipc.Utils;

namespace dotnetCampus.Ipc
{

#if !NETCOREAPP
    // todo 后续需要放在 .NET Core 程序集，因此这个库后续理论上是需要支持 .NET Framework 的
        public abstract class DispatchProxy
        {
            protected abstract object Invoke(MethodInfo targetMethod, object[] args);
        }
#endif

    public class IpcProxy<T> : DispatchProxy
    {
        /// <summary>
        /// 用来标识服务器端的对象
        /// </summary>
        public ulong ObjectId { set; get; }

        public IIpcObjectSerializer IpcObjectSerializer { set; get; }

        public IpcClientProvider IpcClientProvider { set; get; } = null!;

        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            var actualReturnType = GetAndCheckActualReturnType(targetMethod.ReturnType);

            var parameters = targetMethod.GetParameters();

            var parameterTypes = parameters.Select(p => new IpcRequestParameterType(p.ParameterType)).ToArray();

            var parameterList = new List<IpcRequestParameter>(parameterTypes.Length);

            for (var i = 0; i < parameterTypes.Length; i++)
            {
                var ipcRequestParameter = new IpcRequestParameter()
                {
                    ParameterType = parameterTypes[i],
                    Value = args[i]
                };

                parameterList.Add(ipcRequestParameter);
            }

            var genericTypes = targetMethod.GetGenericArguments();

            var genericArgumentList = genericTypes.Select(type => new IpcRequestParameterType(type)).Cast<IpcSerializableType>().ToList();

            var ipcRequest = new IpcRequest()
            {
                MethodName = targetMethod.Name,
                ParameterList = parameterList,
                GenericArgumentList = genericArgumentList,
                ReturnType = new IpcSerializableType(actualReturnType),
                ObjectType = new IpcSerializableType(typeof(T)),
                ObjectId = ObjectId,
            };

            //IpcResponse response = await GetResponseAsync(ipcRequest);
            ////Task<T>

            //TaskCompletionSource<int> t = new TaskCompletionSource<int>();

            //int n = await foo.FooAsync();
            //var re = IpcObjectSerializer.Serialize(ipcRequest);

            // 此方法还没完成，等待下一次实现，有技术实现问题
            throw new NotImplementedException();
        }

        private Type GetAndCheckActualReturnType(Type returnType)
        {
            if (returnType == typeof(Task))
            {
                return typeof(void);
            }
            else if (returnType.BaseType == typeof(Task))
            {
                if (returnType.Name == "Task`1")
                {
                    if (returnType.GenericTypeArguments.Length == 1)
                    {
                        return returnType.GenericTypeArguments[0];
                    }
                }
            }

            throw new ArgumentException($"方法返回值只能是 Task 或 Task 泛型");
        }
    }
}
