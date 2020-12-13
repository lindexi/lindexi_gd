using System;

namespace WatchDagServer.Model
{
    public class RegisterContext
    {
        public ulong Id { set; get; }

        public string Token { get; set; } = null!;

        public uint DelaySecond { get; set; }

        public uint MaxDelayCount { get; set; }

        public uint CurrentDelayCount { get; set; }

        public string? NotifyEmailList { get; set; }

        /// <summary>
        /// 最后一次更新的时间
        /// </summary>
        public DateTimeOffset LastRegisterTime { get; set; }
    }
}