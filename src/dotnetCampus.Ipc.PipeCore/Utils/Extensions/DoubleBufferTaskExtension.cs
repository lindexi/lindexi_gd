using System;
using System.Threading.Tasks;
using dotnetCampus.Threading;

namespace dotnetCampus.Ipc.PipeCore.Utils.Extensions
{
    static class DoubleBufferTaskExtension
    {
        public static async Task AddTaskAsync(this DoubleBufferTask<Func<Task>> doubleBufferTask, Func<Task> task)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();

            doubleBufferTask.AddTask(async () =>
            {
                await task();
                taskCompletionSource.SetResult(true);
            });

            await taskCompletionSource.Task;
        }
    }
}
