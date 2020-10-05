using System.Threading.Tasks;

namespace Ipc
{
    class AckTask
    {
        public AckTask(string clientName, in Ack ack, TaskCompletionSource<bool> task)
        {
            ClientName = clientName;
            Ack = ack;
            Task = task;
        }

        public TaskCompletionSource<bool> Task { get; }

        public Ack Ack { get; }
        public string ClientName { get; }
    }
}