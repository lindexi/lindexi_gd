using System.Net.WebSockets;
using Microsoft.AspNetCore.Mvc;

namespace CodeSignServerMaster.Controllers;

[ApiController]
public class SignController : ControllerBase
{
    [HttpPost]
    [Route("/sign")]
    public async Task Connect()
    {
        using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();

        var arraySegment = new ArraySegment<byte>(new byte[1024]);
        await webSocket.ReceiveAsync(arraySegment, CancellationToken.None);


    }
}