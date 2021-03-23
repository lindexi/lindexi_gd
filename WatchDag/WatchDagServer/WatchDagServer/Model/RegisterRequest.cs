namespace WatchDagServer.Model
{
    public class RegisterRequest
    {
        public string Token { get; set; } = null!;

        public uint DelaySecond { get; set; }

        public uint MaxDelayCount { get; set; }

        public string[]? NotifyEmailList { get; set; }
    }
}