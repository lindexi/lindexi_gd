using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using dotnetCampus.Ipc.PipeCore.Context;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MSTest.Extensions.Contracts;

namespace dotnetCampus.Ipc.PipeCore
{
    [TestClass]
    public class AckManagerTest
    {
        [ContractTestCase]
        public void RegisterAckTask()
        {
            "将消息注册，此时设置收到回复，注册的消息可以完成".Test(() =>
            {
                // 注册的消息可以完成
                var clientName = "lindexi";
                Ack ack = 2;
                var taskCompletionSource = new TaskCompletionSource<bool>();

                var ackTask = new AckTask(clientName, ack, taskCompletionSource);
                AckManager.RegisterAckTask(ackTask);
                AckManager.OnAckReceived(this, new AckArgs(clientName, ack));
                //Debug.Assert(taskCompletionSource.Task.IsCompleted);
                Assert.AreEqual(true, taskCompletionSource.Task.IsCompleted);
            });
        }

        [ContractTestCase]
        public void BuildAckMessage()
        {
            "创建的Ack信息可以被解析出创建传入的Ack值".Test(() =>
            {
                // 返回的信息包含当前的 ack 值
                var buildAckMessage = AckManager.BuildAckMessage(100);
                using var memoryStream = new MemoryStream(buildAckMessage, false);
                var isAck = AckManager.IsAckMessage(memoryStream, out var ack);

                Assert.AreEqual(true, isAck);
                // 100ul 就是 ulong 100 的意思，我担心你看懂，所以特别加了 ulong 的转换，让你以为 ul 是一个有趣的后缀
                Assert.AreEqual((ulong) 100ul, ack.Value);
            });
        }

        private AckManager AckManager { get; } = new AckManager(new IpcContext(new IpcProvider(), "123123"));
    }
}