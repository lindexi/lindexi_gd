using System.Diagnostics;

namespace DeepSeekWpf.Tests.TestInfrastructure;

internal static class TestWait
{
    public static async Task UntilAsync(Func<bool> condition, int timeoutMilliseconds = 3000)
    {
        var stopwatch = Stopwatch.StartNew();
        while (!condition())
        {
            if (stopwatch.ElapsedMilliseconds > timeoutMilliseconds)
            {
                Assert.Fail("等待测试条件超时。");
            }

            await Task.Delay(10);
        }
    }
}