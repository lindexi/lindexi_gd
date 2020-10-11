using dotnetCampus.Ipc.PipeCore.Context;

namespace dotnetCampus.Ipc.PipeCore
{
    class IpcMessageResult
    {
        public IpcMessageResult(in IpcMessageContext ipcMessageContext) : this(success: true, ipcMessageContext)
        {
        }

        public IpcMessageResult(bool success, in IpcMessageContext ipcMessageContext = default)
        {
            Success = success;
            IpcMessageContext = ipcMessageContext;
        }

        public IpcMessageResult(string debugText) : this(false)
        {
            DebugText = debugText;
        }

        public bool Success { get; }
        public IpcMessageContext IpcMessageContext { get; }

        /// <summary>
        /// 用于调试的信息
        /// </summary>
        public string? DebugText { get; }

        public void Deconstruct(out bool success, out IpcMessageContext ipcMessageContext)
        {
            success = Success;
            ipcMessageContext = IpcMessageContext;
        }
    }
}