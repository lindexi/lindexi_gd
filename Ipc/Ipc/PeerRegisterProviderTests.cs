using System.IO;

namespace Ipc
{
    class PeerRegisterProviderTests
    {
        public void Run()
        {
            QijicawkeNerelecairqo();
            //GarkanilearleWudewhanede();
        }

        private void QijicawkeNerelecairqo()
        {
            // 使用发送端之后，能序列化之前的字符串
            var peerRegisterProvider = new PeerRegisterProvider();
            var pipeName = "123";
            var bufferMessageContext = peerRegisterProvider.BuildPeerRegisterMessage(pipeName);
            var memoryStream = new MemoryStream(bufferMessageContext.Length);
            var ipcConfiguration = new IpcConfiguration();

            IpcMessageConverter.WriteAsync(memoryStream, ipcConfiguration.MessageHeader, 10, bufferMessageContext, null!)
                .Wait();

            memoryStream.Position = 0;
            var (success, ipcMessageContext) = IpcMessageConverter.ReadAsync(memoryStream, ipcConfiguration.MessageHeader, new SharedArrayPool()).Result;
            if (success)
            {
                var stream = new ByteListMessageStream(ipcMessageContext);
                if (peerRegisterProvider.TryParsePeerRegisterMessage(stream, out var peerName))
                {
                    if (pipeName.Equals(peerName))
                    {
                    }
                    else
                    {
                    }
                }
                else
                {
                }
            }
        }

        private static void GarkanilearleWudewhanede()
        {
            // 创建的内容可以序列化
            var peerRegisterProvider = new PeerRegisterProvider();
            var pipeName = "123";
            var bufferMessageContext = peerRegisterProvider.BuildPeerRegisterMessage(pipeName);
            var memoryStream = new MemoryStream(bufferMessageContext.Length);

            foreach (var ipcBufferMessage in bufferMessageContext.IpcBufferMessageList)
            {
                memoryStream.Write(ipcBufferMessage.Buffer, ipcBufferMessage.Start, ipcBufferMessage.Count);
            }

            memoryStream.Position = 0;

            if (peerRegisterProvider.TryParsePeerRegisterMessage(memoryStream, out var peerName))
            {
                if (pipeName.Equals(peerName))
                {
                }
                else
                {
                }
            }
            else
            {
            }
        }
    }
}