using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Ipc
{
    internal class AckManagerTest
    {
        private AckManager AckManager { get; } = new AckManager(new IpcContext(new IpcProvider(), "123123"));

        public void Run()
        {
            GelnaihiwiWemlaircilayker();
            //KerekeryawreeDeawhakeewawjear();
        }

        private void GelnaihiwiWemlaircilayker()
        {
            // 注册的消息可以完成
            var clientName = "lindexi";
            Ack ack = 2;
            var taskCompletionSource = new TaskCompletionSource<bool>();
            
            var ackTask = new AckTask(clientName, ack, taskCompletionSource);
            AckManager.RegisterAckTask(ackTask);
            AckManager.OnAckReceived(this,new AckArgs(clientName,ack));
            Debug.Assert(taskCompletionSource.Task.IsCompleted);
        }

        private void KerekeryawreeDeawhakeewawjear()
        {
            // 返回的信息包含当前的 ack 值

            var buildAckMessage = AckManager.BuildAckMessage(100);
            using var memoryStream = new MemoryStream(buildAckMessage, false);
            if (AckManager.IsAckMessage(memoryStream, out var ack))
            {
                if (ack.Value == 100)
                {
                }
            }
        }
    }
}