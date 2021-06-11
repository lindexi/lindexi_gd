using dotnetCampus.Cli;

namespace dotnetCampus.Ipc.WpfDemo
{
    public class Options
    {
        /// <summary>
        /// 本机的服务名
        /// </summary>
        [Option(longName: nameof(ServerName))]
        public string? ServerName { get; set; }

        /// <summary>
        /// 对方的服务名
        /// </summary>
        [Option(longName: nameof(PeerName))]
        public string? PeerName { get; set; }
    }
}
