namespace dotnetCampus.Ipc.PipeCore.Context
{
    class IpcLogger : ILogger
    {
        public IpcLogger(IpcContext ipcContext)
        {
            IpcContext = ipcContext;
        }

        public override string ToString()
        {
            return $"[{IpcContext.PipeName}]";
        }

        private IpcContext IpcContext { get; }
    }
}
