using System.IO;
using System.Threading.Tasks;

namespace Ipc
{
    internal class IpcMessageConverterTest
    {
        public async void Run()
        {
            await ChawewhuhiLafihoweredal();
        }

        public async Task ChawewhuhiLafihoweredal()
        {
            // 写入的数据和读取的相同
            using var memoryStream = new MemoryStream();

            var ipcConfiguration = new IpcConfiguration();
            ulong ack = 10;
            var buffer = new byte[] {0x12, 0x12, 0x00};
            await IpcMessageConverter.WriteAsync(memoryStream, ipcConfiguration.MessageHeader, ack, buffer, 0,
                buffer.Length,"test",null!);

            memoryStream.Position = 0;
            var (success, ipcMessageContext) = await IpcMessageConverter.ReadAsync(memoryStream,
                ipcConfiguration.MessageHeader, new SharedArrayPool());
            if (success)
            {
            }

            if (ipcMessageContext.Ack.Value == ack)
            {
            }
        }
    }
}