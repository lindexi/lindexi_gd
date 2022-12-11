namespace PackageManager.Server.Context;

public static class ResponseErrorCode
{
    public static ErrorCode Ok => new ErrorCode(0, "OK");

    /// <summary>
    /// 不支持此客户端版本
    /// </summary>
    public static ErrorCode DoNotSupportClientVersion => new ErrorCode(1000, "不支持此客户端版本");
}