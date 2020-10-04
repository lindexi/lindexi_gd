namespace Ipc
{
    public class IpcContext
    {
        public const string PipeName = "1231";

        internal AckManager AckManager { get; } = new AckManager();
        
        internal IpcConfiguration IpcConfiguration { get; set; } = new IpcConfiguration();
    }
}