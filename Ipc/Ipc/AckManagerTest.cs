using System.IO;

namespace Ipc
{
    internal class AckManagerTest
    {
        private AckManager AckManager { get; } = new AckManager(new IpcContext(new IpcProvider(), "123123"));

        public void Run()
        {
            KerekeryawreeDeawhakeewawjear();
        }

        private void KerekeryawreeDeawhakeewawjear()
        {
            // 返回的信息包含当前的 ack 值

            var buildAckMessage = AckManager.BuildAckMessage(100);
            using var memoryStream = new MemoryStream(buildAckMessage, false);
            if (AckManager.IsAckMessage(memoryStream, out var ack))
                if (ack.Value == 100)
                {
                }
        }
    }
}