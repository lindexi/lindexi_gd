using System;
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
            "重复注册相同编号的消息，提示错误".Test(() =>
            {
                var clientName = "lindexi";
                Ack ack = 20;
                var taskCompletionSource = new TaskCompletionSource<bool>();

                var ackTask = new AckTask(clientName, ack, taskCompletionSource, "调试");
                AckManager.RegisterAckTask(ackTask);
                Assert.ThrowsException<ArgumentException>(() => { AckManager.RegisterAckTask(ackTask); });
            });

            "将消息注册，如果没有收到回复，那么注册的任务依然没有完成".Test(() =>
            {
                // 注册的消息可以完成
                var clientName = "lindexi";
                Ack ack = 2;
                var taskCompletionSource = new TaskCompletionSource<bool>();

                var ackTask = new AckTask(clientName, ack, taskCompletionSource, "调试");
                AckManager.RegisterAckTask(ackTask);
                Assert.AreEqual(false, taskCompletionSource.Task.IsCompleted);
            });

            "将消息注册，此时设置收到回复，注册的消息可以完成".Test(() =>
            {
                // 注册的消息可以完成
                var clientName = "lindexi";
                Ack ack = 2;
                var taskCompletionSource = new TaskCompletionSource<bool>();

                var ackTask = new AckTask(clientName, ack, taskCompletionSource, "调试");
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

            "传入不属于 Ack 的信息，可以返回不是 Ack 信息".Test(() =>
            {
                using var memoryStream = new MemoryStream();
                var streamWriter = new StreamWriter(memoryStream);
                streamWriter.WriteLine("林德熙是逗比");
                const long position = 2;
                memoryStream.Position = position;

                var isAck = AckManager.IsAckMessage(memoryStream, out var ack);
                Assert.AreEqual(false, isAck);
                // 如果读取返回不是 Ack 那么将 Stream 设置回传入的 Position 的值
                Assert.AreEqual(position, memoryStream.Position);
            });
        }

        private AckManager AckManager { get; } = new AckManager(new IpcContext(new IpcProvider(), "123123"));
    }
}
