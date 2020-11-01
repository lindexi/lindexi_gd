using System.Collections.Generic;

namespace dotnetCampus.Ipc.Context
{
    /// <summary>
    /// 发送给服务器的请求数据
    /// </summary>
    /// Copy From: https://github.com/jacqueskang/IpcServiceFramework.git
    public class IpcRequest
    {
        public IpcSerializableType ObjectType { set; get; }

        /// <summary>
        /// 用来标识服务器端的对象
        /// </summary>
        public ulong ObjectId { set; get; }

        public string MethodName { get; set; }

        public List<IpcRequestParameter> ParameterList { set; get; }

        public List<IpcSerializableType> GenericArgumentList { set; get; }

        public IpcSerializableType ReturnType { set; get; }
    }
}
