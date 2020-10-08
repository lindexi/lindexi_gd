using System.IO;
using System.Threading.Tasks;
using dotnetCampus.Ipc.PipeCore.Context;
using dotnetCampus.Ipc.PipeCore.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MSTest.Extensions.Contracts;

namespace dotnetCampus.Ipc.PipeCore
{
    [TestClass]
    public class IpcMessageConverterTest
    {
        [ContractTestCase]
        public void IpcMessageConverterWriteAsync()
        {
            "写入的数据和读取的相同，可以读取到写入的数据".Test(async () =>
            {
                // 写入的数据和读取的相同
                using var memoryStream = new MemoryStream();

                var ipcConfiguration = new IpcConfiguration();
                ulong ack = 10;
                var buffer = new byte[] { 0x12, 0x12, 0x00 };
                await IpcMessageConverter.WriteAsync(memoryStream, ipcConfiguration.MessageHeader, ack, buffer, 0,
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