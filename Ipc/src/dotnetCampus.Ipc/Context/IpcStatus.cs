namespace dotnetCampus.Ipc.Context
{
    /// <summary>
    /// 从服务器返回的值
    /// </summary>
    /// Copy From: https://github.com/jacqueskang/IpcServiceFramework.git
    public enum IpcStatus : int
    {
        Unknown = 0,
        Ok = 200,
        BadRequest = 400,
        InternalServerError = 500,
    }
}
