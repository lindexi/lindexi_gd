namespace dotnetCampus.Ipc.PipeCore.Context
{
    /// <summary>
    /// 用于作为命令类型，用于框架的命令和业务的命令
    /// </summary>
    internal enum IpcMessageCommandType : ushort
    {
        Business = 0,

        Unknown = ushort.MaxValue,
    }
}