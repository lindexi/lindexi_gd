namespace dotnetCampus.Ipc.PipeCore.Context
{
    /// <summary>
    /// 包含多条信息的上下文
    /// </summary>
    /// 不开放，因为我不认为上层能用对
    readonly struct IpcBufferMessageContext
    {
        /// <summary>
        /// 创建包含多条信息的上下文
        /// </summary>
        /// <param name="summary">表示写入的是什么内容，用于调试</param>
        /// <param name="ipcBufferMessageList"></param>
        public IpcBufferMessageContext(string summary, params IpcBufferMessage[] ipcBufferMessageList)
        {
            Summary = summary;
            IpcBufferMessageList = ipcBufferMessageList;
        }

        public IpcBufferMessage[] IpcBufferMessageList { get; }

        /// <summary>
        /// 表示内容是什么用于调试
        /// </summary>
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