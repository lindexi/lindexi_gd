using System.IO;
using dotnetCampus.Ipc.PipeCore;
using dotnetCampus.Ipc.PipeCore.Context;
using dotnetCampus.Ipc.PipeCore.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MSTest.Extensions.Contracts;

namespace dotnetCampus.Ipc.Tests
{
    [TestClass]
    public class PeerRegisterProviderTests
    {
        [ContractTestCase]
        public void BuildPeerRegisterMessage()
        {
            "如果注册消息的内容添加了其他内容，不会读取到不属于注册消息的内容".Test(() =>
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

                // 写入其他内容
                var streamWriter = new StreamWriter(memoryStream);
                streamWriter.Write("林德熙是逗比");
                streamWriter.Flush();

                memoryStream.Position = 0;

                var success = peerRegisterProvider.TryParsePeerRegisterMessage(memoryStream, out var peerName);

                Assert.AreEqual(true, success);
                Assert.AreEqual(pipeName, peerName);
            });

            "如果消息不是对方的注册消息，那么将不修改Stream的起始".Test(() =>
            {
                var peerRegisterProvider = new PeerRegisterProvider();
                var memoryStream = new MemoryStream();
                for (int i = 0; i < 1000; i++)
                {
                    memoryStream.WriteByte(0x00);
                }

                const int position = 10;
                memoryStream.Position = position;
                var isPeerRegisterMessage = peerRegisterProvider.TryParsePeerRegisterMessage(memoryStream, out _);
                Assert.AreEqual(false, isPeerRegisterMessage);
                Assert.AreEqual(position, memoryStream.Position);
            });

            "使用发送端之后，能序列化之前的字符串".Test(async () =>
            {
                var peerRegisterProvider = new PeerRegisterProvider();
                var pipeName = "123";
                var bufferMessageContext = peerRegisterProvider.BuildPeerRegisterMessage(pipeName);
                var memoryStream = new MemoryStream(bufferMessageContext.Length);
                var ipcConfiguration = new IpcConfiguration();

                await IpcMessageConverter.WriteAsync(memoryStream, ipcConfiguration.MessageHeader, 10,
                    bufferMessageContext, null!);

                memoryStream.Position = 0;
                var (success, ipcMessageContext) = await IpcMessageConverter.ReadAsync(memoryStream,
                    ipcConfiguration.MessageHeader, new SharedArrayPool());

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
