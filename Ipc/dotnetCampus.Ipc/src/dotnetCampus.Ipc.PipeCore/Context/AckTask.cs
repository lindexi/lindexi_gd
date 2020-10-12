using System.Threading.Tasks;

namespace dotnetCampus.Ipc.PipeCore.Context
{
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