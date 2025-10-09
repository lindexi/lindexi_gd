namespace CodeSignServerMaster
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();

            var app = builder.Build();

            // Configure the HTTP request pipeline.

            app.UseWebSockets(new WebSocketOptions()
            {
                
            });

            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/sign")
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                        var arraySegment = new ArraySegment<byte>(new byte[1024]);
                        await webSocket.ReceiveAsync(arraySegment, CancellationToken.None);
                    }
                    else
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    }
                }
                else
                {
                    await next(context);
                }

            });

            app.MapControllers();

            app.Run();
        }
    }
}
