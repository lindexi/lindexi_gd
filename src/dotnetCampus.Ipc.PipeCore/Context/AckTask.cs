using System.Threading.Tasks;

namespace dotnetCampus.Ipc.PipeCore.Context
{
    /// <summary>
    /// 回复 Ack 的任务，用于在 <see cref="AckManager"/> 收到回复的时候设置任务完成
    /// </summary>
    class AckTask
    {
        public AckTask(string peerName, in Ack ack, TaskCompletionSource<bool> task)
        {
            PeerName = peerName;
            Ack = ack;
            Task = task;
        }

        public TaskCompletionSource<bool> Task { get; }

        public Ack Ack { get; }
        public string PeerName { get; }
    }
}