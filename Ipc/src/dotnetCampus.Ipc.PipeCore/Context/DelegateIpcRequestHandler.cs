using System;
using System.Threading.Tasks;
using dotnetCampus.Ipc.Abstractions;

namespace dotnetCampus.Ipc.PipeCore.Context
{
    /// <summary>
    /// 基于委托的 IPC 请求处理
    /// </summary>
    public class DelegateIpcRequestHandler : IIpcRequestHandler
    {
        /// <summary>
        /// 创建基于委托的 IPC 请求处理
        /// </summary>
        /// <param name="handler"></param>
        public DelegateIpcRequestHandler(Func<IIpcRequestContext, Task<IIpcHandleRequestMessageResult>> handler)
        {
            _handler = handler;
        }

        /// <summary>
        /// 创建基于委托的 IPC 请求处理
        /// </summary>
        public DelegateIpcRequestHandler(Func<IIpcRequestContext, IIpcHandleRequestMessageResult> handler)
        {
            _handler = c => Task.FromResult(handler(c));
        }

        Task<IIpcHandleRequestMessageResult> IIpcRequestHandler.HandleRequestMessage(IIpcRequestContext requestContext)
        {
            return _handler(requestContext);
        }

        private readonly Func<IIpcRequestContext, Task<IIpcHandleRequestMessageResult>> _handler;
    }
}
