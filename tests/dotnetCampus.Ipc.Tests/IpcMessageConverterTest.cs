using System.IO;
using dotnetCampus.Ipc.PipeCore;
using dotnetCampus.Ipc.PipeCore.Context;
using dotnetCampus.Ipc.PipeCore.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MSTest.Extensions.Contracts;

namespace dotnetCampus.Ipc.Tests
{
    [TestClass]
    public class IpcMessageConverterTest
    {
        [ContractTestCase]
        public void IpcMessageConverterWriteAsync()
        {
            "读取消息头不对的数据，可以返回读取失败".Test(async () =>
            {
                using var memoryStream = new MemoryStream();

                ulong ack = 10;
                var buffer = new byte[] { 0x12, 0x12, 0x00 };
                var messageHeader = new byte[] { 0x00, 0x00 };
                var breakMessageHeader = new byte[] { 0x00, 0x01 };

                await IpcMessageConverter.WriteAsync(memoryStream, messageHeader, ack, IpcMessageCommandType.Unknown,
                    buffer, 0,
                    buffer.Length, "test", null!);

                memoryStream.Position = 0;
                var ipcMessageResult = await IpcMessageConverter.ReadAsync(memoryStream,
                    breakMessageHeader, new SharedArrayPool());
                var success = ipcMessageResult.Success;
                var ipcMessageCommandType = ipcMessageResult.IpcMessageCommandType;

                Assert.AreEqual(false, success);
                Assert.AreEqual(IpcMessageCommandType.Unknown, ipcMessageCommandType);
            });

            "读取消息头长度不对的数据，可以返回读取失败".Test(async () =>
            {
                using var memoryStream = new MemoryStream();

                var ipcConfiguration = new IpcConfiguration();
                ulong ack = 10;
                var buffer = new byte[] { 0x12, 0x12, 0x00 };
                var messageHeader = new byte[] { 0x00, 0x00 };
                await IpcMessageConverter.WriteAsync(memoryStream, messageHeader, ack, IpcMessageCommandType.Unknown,
                    buffer, 0,
                    buffer.Length, "test", null!);

                memoryStream.Position = 0;
                var ipcMessageResult = await IpcMessageConverter.ReadAsync(memoryStream,
                    ipcConfiguration.MessageHeader, new SharedArrayPool());
                var success = ipcMessageResult.Success;
                var ipcMessageCommandType = ipcMessageResult.IpcMessageCommandType;

                Assert.AreEqual(false, success);
                Assert.AreEqual(IpcMessageCommandType.Unknown, ipcMessageCommandType);
            });

            "写入的数据和读取的相同，可以读取到写入的数据".Test(async () =>
            {
                // 写入的数据和读取的相同
                using var memoryStream = new MemoryStream();

                var ipcConfiguration = new IpcConfiguration();
                ulong ack = 10;
                var buffer = new byte[] { 0x12, 0x12, 0x00 };
                await IpcMessageConverter.WriteAsync(memoryStream, ipcConfiguration.MessageHeader, ack,
                    IpcMessageCommandType.Unknown, buffer, 0,
                    buffer.Length, "test", null!);

                memoryStream.Position = 0;
                var (success, ipcMessageContext) = await IpcMessageConverter.ReadAsync(memoryStream,
                    ipcConfiguration.MessageHeader, new SharedArrayPool());

                Assert.AreEqual(true, success);
                Assert.AreEqual(ack, ipcMessageContext.Ack.Value);
            });
        }
    }
}
