using System;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace dotnetCampus.Ipc
{
    public class IpcObjectJsonSerializer : IIpcObjectSerializer
    {
        public byte[] Serialize(object obj)
        {
            var json = JsonConvert.SerializeObject(obj);
            return Encoding.UTF8.GetBytes(json);
        }

        public T Deserialize<T>(byte[] byteList)
        {
            var json = Encoding.UTF8.GetString(byteList);
            return JsonConvert.DeserializeObject<T>(json);
        }
    }

    public class IpcClientProvider
    {
        public T GetObject<T>()
        {
            var obj= DispatchProxy.Create<T, IpcProxy<T>>();
            var ipcProxy = obj as IpcProxy<T>;
            ipcProxy.IpcClientProvider = this;
            return obj;
        }
    }

    public class IpcProxy<T> : DispatchProxy
    {
        public IpcClientProvider IpcClientProvider { set; get; } = null!;

        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            var actualReturnType = GetAndCheckActualReturnType(targetMethod.ReturnType);

            return default!;
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

            throw new ArgumentException($"方法返回值只能是 Task 或 Task 泛形");
        }
    }
}
