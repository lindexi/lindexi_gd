using System;
using System.Threading.Tasks;

namespace CurwhonairwhiqeQairkerejikur
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var btcReceivedTask = new TaskCompletionSource<bool>();

            BtcMessage.KeyReceived += (sender, eventArgs) =>
            {
                btcReceivedTask.SetResult(true);
            };

            var worldBrokeTask = new WorldBrokeTask();

            HackTeam.PeekKey();

            await btcReceivedTask.Task;

            Missiles.Fire();

            await worldBrokeTask.WaitWorldBroke();

            Console.WriteLine("Hello World!");

            Console.Read();
        }
    }

    public class WorldBrokeTask
    {
        public WorldBrokeTask()
        {
            World.Broke += World_Broke;
        }

        private void World_Broke(object sender, EventArgs e)
        {
            World.Broke -= World_Broke;
            TaskCompletionSource.SetResult(true);
        }

        public Task WaitWorldBroke()
        {
            return TaskCompletionSource.Task;
        }

        private TaskCompletionSource<bool> TaskCompletionSource { get; } = new TaskCompletionSource<bool>();
    }

    static class Missiles
    {
        public static void Fire()
        {
            Task.Run(World.OnBroke);
        }
    }

    static class World
    {
        public static event EventHandler Broke;

        public static void OnBroke()
        {
            Broke?.Invoke(null, EventArgs.Empty);
        }
    }

    static class BtcMessage
    {
        public static event EventHandler KeyReceived;

        public static void OnKeyReceived()
        {
            KeyReceived?.Invoke(null, EventArgs.Empty);
        }
    }

    static class HackTeam
    {
        public static void PeekKey()
        {
            Task.Run(BtcMessage.OnKeyReceived);
        }
    }
}
