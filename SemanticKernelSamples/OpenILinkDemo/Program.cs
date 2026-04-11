using OpenILink.SDK;

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

using var client = OpenILinkClient.Create();

if (string.IsNullOrWhiteSpace(client.Token))
{
    var login = await client.LoginWithQrAsync(ShowQrCode, OnScanned);
    if (!login.Connected)
    {
        Console.Error.WriteLine($"登录失败: {login.Message}");
        return;
    }

    Console.WriteLine($"Token={login.BotToken}");
}
await client.MonitorAsync(HandleMessageAsync, new MonitorOptions
{
    InitialBuffer = null, //ReadText(bufferPath),
    OnBufferUpdated = SaveBuffer,
    OnError = ReportError,
    OnSessionExpired = ReportSessionExpired
});

Console.WriteLine("Hello, World!");


async void ShowQrCode(string qrCodeImageDownloadUrl)
{
    Process.Start(new ProcessStartInfo(qrCodeImageDownloadUrl)
    {
        UseShellExecute = true
    });

    //Console.WriteLine($"二维码下载地址：{qrCodeImageDownloadUrl}");
    //using var httpClient = new HttpClient();
    //await using var response = await httpClient.GetStreamAsync(qrCodeImageDownloadUrl);
    //var qrFile = Path.Join(AppContext.BaseDirectory, $"{Path.GetRandomFileName()}.png");
    //var fileStream = File.Create(qrFile);
    //await using (fileStream)
    //{
    //    await response.CopyToAsync(fileStream);
    //}

    //if (OperatingSystem.IsWindows())
    //{
    //    Process.Start("explorer", qrFile);
    //}
}

void OnScanned()
{
    Console.WriteLine("已扫码，请在微信端确认。");
}

Task HandleMessageAsync(WeixinMessage message)
{
    var text = message.ExtractText();
    if (string.IsNullOrWhiteSpace(text))
    {
        return Task.CompletedTask;
    }

    Console.WriteLine($"[{message.FromUserId}] {text}");
    return client.ReplyTextAsync(message, $"echo: {text}");
}

void SaveBuffer(string buffer)
{

}

void ReportError(Exception exception)
{
    Console.Error.WriteLine(exception.Message);
}

void ReportSessionExpired()
{
    Console.Error.WriteLine("会话过期，请重新登录。");
}

static string ReadText(string path)
{
    return File.Exists(path) ? File.ReadAllText(path).Trim() : string.Empty;
}