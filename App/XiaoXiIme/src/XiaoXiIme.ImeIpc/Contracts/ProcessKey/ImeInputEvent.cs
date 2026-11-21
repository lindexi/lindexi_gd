namespace XiaoXiIme.ImeIpc;

/// <summary>Delivers a key independently of its eventual input result.</summary>
public sealed record ImeInputEvent(string ReplyPipe, ImeProcessKeyRequest Request);

/// <summary>Confirms delivery without reporting a conversion result.</summary>
public sealed record ImeDeliveryReceipt
{
    internal static readonly ImeDeliveryReceipt Accepted = new();
}
