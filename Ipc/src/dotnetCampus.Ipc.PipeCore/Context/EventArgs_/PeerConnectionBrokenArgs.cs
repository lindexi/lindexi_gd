using System;
using dotnetCampus.Ipc.Abstractions.Context;

namespace dotnetCampus.Ipc.PipeCore.Context
{
    /// <summary>
    /// 对方连接断开事件参数
    /// </summary>
    public class PeerConnectionBrokenArgs : EventArgs, IPeerConnectionBrokenArgs
    {

    }
}
