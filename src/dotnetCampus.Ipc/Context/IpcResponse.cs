using System;
using System.Runtime.Serialization;

namespace dotnetCampus.Ipc.Context
{
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
}
