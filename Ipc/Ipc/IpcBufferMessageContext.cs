namespace Ipc
{
    readonly struct IpcBufferMessageContext
    {
        public IpcBufferMessageContext(params IpcBufferMessage[] ipcBufferMessageList)
        {
            IpcBufferMessageList = ipcBufferMessageList;
        }

        public IpcBufferMessage[] IpcBufferMessageList { get; }

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