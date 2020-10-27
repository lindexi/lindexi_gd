using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
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

    /// <summary>
    /// 发送给服务器的请求数据
    /// </summary>
    /// Copy From: https://github.com/jacqueskang/IpcServiceFramework.git
    public class IpcRequest
    {
        public Type ObjectType { set; get; }

        /// <summary>
        /// 用来标识服务器端的对象
        /// </summary>
        public ulong ObjectId { set; get; }

        public string MethodName { get; set; }

        public List<IpcRequestParameter> ParameterList { set; get; }

        public List<Type> GenericArgumentList { set; get; }

        public Type ReturnType { set; get; }
    }

    /// <summary>
    /// 从服务器返回的信息
    /// </summary>
    /// Copy From: https://github.com/jacqueskang/IpcServiceFramework.git
    public class IpcResponse
    {
        public static IpcResponse Success(object data)
            => new IpcResponse(IpcStatus.Ok, data, null, null);

        public static IpcResponse BadRequest()
            => new IpcResponse(IpcStatus.BadRequest, null, null, null);

        public static IpcResponse BadRequest(string errorDetails)
            => new IpcResponse(IpcStatus.BadRequest, null, errorDetails, null);

        public static IpcResponse BadRequest(string errorDetails, Exception innerException)
            => new IpcResponse(IpcStatus.BadRequest, null, errorDetails, innerException);

        public static IpcResponse InternalServerError()
            => new IpcResponse(IpcStatus.InternalServerError, null, null, null);

        public static IpcResponse InternalServerError(string errorDetails)
            => new IpcResponse(IpcStatus.InternalServerError, null, errorDetails, null);

        public static IpcResponse InternalServerError(string errorDetails, Exception innerException)
            => new IpcResponse(IpcStatus.InternalServerError, null, errorDetails, innerException);

        public IpcResponse(
            IpcStatus status,
            object data,
            string errorMessage,
            Exception innerException)
        {
            Status = status;
            Data = data;
            ErrorMessage = errorMessage;
            InnerException = innerException;
        }

        [DataMember]
        public IpcStatus Status { get; }

        [DataMember]
        public object Data { get; }

        [DataMember]
        public string ErrorMessage { get; set; }

        [DataMember]
        public Exception InnerException { get; }

        public bool Succeed() => Status == IpcStatus.Ok;
    }

    /// <summary>
    /// 从服务器返回的值
    /// </summary>
    /// Copy From: https://github.com/jacqueskang/IpcServiceFramework.git
    public enum IpcStatus : int
    {
        Unknown = 0,
        Ok = 200,
        BadRequest = 400,
        InternalServerError = 500,
    }

    public class IpcRequestParameter
    {
        public Type ParameterType { set; get; }

        public object Value { set; get; }
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
