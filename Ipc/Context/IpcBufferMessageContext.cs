namespace Ipc
{
    readonly struct IpcBufferMessageContext
    {
        public IpcBufferMessageContext(string summary, params IpcBufferMessage[] ipcBufferMessageList)
        {
            Summary = summary;
            IpcBufferMessageList = ipcBufferMessageList;
        }

        public IpcBufferMessage[] IpcBufferMessageList { get; }

        public string Summary { get; }

        public int Length
        {
            get
            {
                var length = 0;
                foreach (var ipcBufferMessage in IpcBufferMessageList)
                {
                    length += ipcBufferMessage.Count;
                }

                return length;
            }
        }
    }
}