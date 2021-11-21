namespace dotnetCampus.Ipc.PipeCore.Context
{
    class IpcMessageResult
    {
        public IpcMessageResult(bool success, in IpcMessageContext ipcMessageContext = default,
            in IpcMessageCommandType ipcMessageCommandType = IpcMessageCommandType.Unknown)
        {
            Success = success;
            IpcMessageContext = ipcMessageContext;
            IpcMessageCommandType = ipcMessageCommandType;
        }

        public IpcMessageResult(string debugText) : this(success: false)
        {
            DebugText = debugText;
        }

        public bool Success { get; }
        public IpcMessageContext IpcMessageContext { get; }

        /// <summary>
        /// 用于调试的信息
        /// </summary>
        public string? DebugText { get; }

        internal IpcMessageCommandType IpcMessageCommandType { get; }

        public void Deconstruct(out bool success, out IpcMessageContext ipcMessageContext)
        {
            success = Success;
            ipcMessageContext = IpcMessageContext;
        }
    }
}
