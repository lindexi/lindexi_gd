namespace Ipc
{
    public class IpcContext
    {
        public IpcContext(IpcProvider ipcProvider, string clientName)
        {
            IpcProvider = ipcProvider;
            ClientName = clientName;

            AckManager = new AckManager(this);
        }

        public const string DefaultPipeName = "1231";

        internal AckManager AckManager { get; }

        internal IpcConfiguration IpcConfiguration { get; set; } = new IpcConfiguration();

        internal IpcProvider IpcProvider { get; }
        public string ClientName { get; }
    }
}