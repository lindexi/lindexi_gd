using System.IO;

namespace Ipc
{
    class AckManagerTest
    {
        public void Run()
        {
            KerekeryawreeDeawhakeewawjear();
        }

        private void KerekeryawreeDeawhakeewawjear()
        {
            // 返回的信息包含当前的 ack 值

            var buildAckMessage = AckManager.BuildAckMessage(100);
            using var memoryStream = new MemoryStream(buildAckMessage, false);
            if (AckManager.IsAckMessage(memoryStream,out var ack))
            {
                if (ack==100)
                {
                    
                }
            }
        }

        private AckManager AckManager { get; } = new AckManager();
    }
}