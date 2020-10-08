using System.IO;
using dotnetCampus.Ipc.PipeCore.Context;
using dotnetCampus.Ipc.PipeCore.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MSTest.Extensions.Contracts;

namespace dotnetCampus.Ipc.PipeCore
{
    [TestClass]
    public class PeerRegisterProviderTests
    {
        [ContractTestCase]
        public void BuildPeerRegisterMessage()
        {
            "使用发送端之后，能序列化之前的字符串".Test(async () =>
            {
                var peerRegisterProvider = new PeerRegisterProvider();
                var pipeName = "123";
                var bufferMessageContext = peerRegisterProvider.BuildPeerRegisterMessage(pipeName);
                var memoryStream = new MemoryStream(bufferMessageContext.Length);
                var ipcConfiguration = new IpcConfiguration();

                await IpcMessageConverter.WriteAsync(memoryStream, ipcConfiguration.MessageHeader, 10, bufferMessageContext, null!);

                memoryStream.Position = 0;
                var (success, ipcMessageContext) = await IpcMessageConverter.ReadAsync(memoryStream, ipcConfiguration.MessageHeader, new SharedArrayPool());

                Assert.AreEqual(true, success);

                var stream = new ByteListMessageStream(ipcMessageContext);
                success = peerRegisterProvider.TryParsePeerRegisterMessage(stream, out var peerName);

                Assert.AreEqual(true, success);

                Assert.AreEqual(pipeName, peerName);
            });

            "创建的注册服务器名内容可以序列化，序列化之后可以反序列化出服务器名".Test(() =>
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

                var success = peerRegisterProvider.TryParsePeerRegisterMessage(memoryStream, out var peerName);

                Assert.AreEqual(true, success);
                Assert.AreEqual(pipeName, peerName);
            });
        }
    }
}