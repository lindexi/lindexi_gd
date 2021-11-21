using dotnetCampus.Ipc.Abstractions;
using dotnetCampus.Ipc.PipeCore.Context;

namespace dotnetCampus.Ipc.PipeCore.IpcPipe
{
    /// <summary>
    /// 关联 <see cref="IpcMessageRequestManager"/> 和 <see cref="IIpcRequestHandler"/> 的联系
    /// </summary>
    class IpcRequestHandlerProvider
    {
        public IpcRequestHandlerProvider(IpcContext ipcContext)
        {
            IpcContext = ipcContext;
        }

        public IpcContext IpcContext { get; }

        /// <summary>
        /// 处理请求消息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// 有三步
        /// 1. 取出消息和上下文里面带的 <see cref="IIpcRequestHandler"/> 用于处理消息
        /// 2. 构建出 <see cref="IIpcRequestContext"/> 传入到 <see cref="IIpcRequestHandler"/> 处理
        /// 3. 将 <see cref="IIpcRequestHandler"/> 的返回值发送给到客户端
        public async void HandleRequest(PeerProxy sender, IpcClientRequestArgs args)
        {
            var requestMessage = args.IpcBufferMessage;
            var peerProxy = sender;

            var ipcRequestContext = new IpcRequestMessageContext(requestMessage, peerProxy);

            // 处理消息
            // 优先从 Peer 里面找处理的方法，这样上层可以对某个特定的 Peer 做不同的处理
            // Todo 需要设计这部分 API 现在因为没有 API 的设计，先全部走 DefaultIpcRequestHandler 的逻辑
            IIpcRequestHandler ipcRequestHandler = IpcContext.IpcConfiguration.DefaultIpcRequestHandler;
            var result = await ipcRequestHandler.HandleRequestMessage(ipcRequestContext);

            // 构建信息回复
            var responseManager = IpcContext.IpcMessageResponseManager;
            var responseMessage = responseManager.CreateResponseMessage(args.MessageId, result.ReturnMessage);

            // 发送回客户端
            await peerProxy.IpcClientService.WriteMessageAsync(responseMessage);
        }
    }
}

