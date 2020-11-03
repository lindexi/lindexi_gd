using System;

namespace dotnetCampus.Ipc.PipeCore.Context
{
    public class IpcContext
    {
        public const string DefaultPipeName = "1231";

        public IpcContext(IpcProvider ipcProvider, string pipeName)
        {
            IpcProvider = ipcProvider;
            PipeName = pipeName;

            AckManager = new AckManager(this);
        }

        internal AckManager AckManager { get; }

        internal IpcConfiguration IpcConfiguration { get; set; } = new IpcConfiguration();

        internal IpcProvider IpcProvider { get; }
        public string PipeName { get; }

        internal PeerRegisterProvider PeerRegisterProvider { get; } = new PeerRegisterProvider();

        internal ILogger Logger { get; } = null!;
    }

    public interface ILogger
    {

    }

    internal static class LoggerExtension
    {
        public static void Debug(this ILogger logger, string message)
        {
            Console.WriteLine(message);
        }

        public static void Trace(this ILogger logger, string message)
        {
            Console.WriteLine(message);
        }
    }

}