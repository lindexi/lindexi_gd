// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Security.Cryptography;

if (!OperatingSystem.IsWindows())
{
    return;
}

// 用户加密
byte[] additionalEntropy = "额外加密密码"u8.ToArray(); // 额外加密密码，可以是空

var data = "123123"u8.ToArray();
var encryptedData = ProtectedData.Protect(data, additionalEntropy, DataProtectionScope.CurrentUser);
// 加密之后的内容，长度更长了
Debug.Assert(data.Length<encryptedData.Length);

try
{
    var errorData = ProtectedData.Unprotect(encryptedData, "错误的加密密码"u8.ToArray(), DataProtectionScope.CurrentUser);
}
catch (System.Security.Cryptography.CryptographicException e)
{
    Debug.Assert(((uint) e.HResult) == 0x8007000d);
}

var decryptedData = ProtectedData.Unprotect(encryptedData, additionalEntropy, DataProtectionScope.CurrentUser);

Debug.Assert(data.SequenceEqual(decryptedData));

Console.WriteLine("Hello, World!");
