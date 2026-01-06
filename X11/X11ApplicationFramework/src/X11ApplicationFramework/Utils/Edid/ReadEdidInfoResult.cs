using System.Collections.Generic;

namespace X11ApplicationFramework.Utils.Edid;

public readonly record struct ReadEdidInfoResult(bool IsSuccess, string ErrorMessage, IReadOnlyList<EdidInfo> EdidInfoList)
{
    public static ReadEdidInfoResult Success(IReadOnlyList<EdidInfo> edidInfoList) => new ReadEdidInfoResult(true, "Success", edidInfoList);
    public static ReadEdidInfoResult Fail(string errorMessage) => new ReadEdidInfoResult(false, errorMessage, []);
}