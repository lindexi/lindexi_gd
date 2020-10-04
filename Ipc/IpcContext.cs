namespace Ipc
{
    public class IpcContext
    {
        public const string DefaultPipeName = "1231";

        public IpcContext(IpcProvider ipcProvider, string clientName)
        {
            IpcProvider = ipcProvider;
            ClientName = clientName;

            AckManager = new AckManager(this);
        }

        internal AckManager AckManager { get; }

        internal IpcConfiguration IpcConfiguration { get; set; } = new IpcConfiguration();

        internal IpcProvider IpcProvider { get; }
        public string ClientName { get; }
    }
}