using System;
using System.Reflection;
using System.Threading.Tasks;

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
