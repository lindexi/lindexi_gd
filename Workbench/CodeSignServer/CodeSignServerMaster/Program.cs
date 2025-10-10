using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;

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
                KeepAliveInterval = TimeSpan.FromSeconds(1),
                //KeepAliveTimeout = TimeSpan.FromSeconds(10),
            });

            var signSlaveList = new List<SignSlave>();

            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/task")
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                        var arraySegment = new ArraySegment<byte>(new byte[10240]);
                        var result = await webSocket.ReceiveAsync(arraySegment, CancellationToken.None);
                        var nameSpan = arraySegment.AsSpan(0, result.Count);
                        var name = Encoding.UTF8.GetString(nameSpan);

                        var semaphoreSlim = new SemaphoreSlim(1);

                        var signSlave = new SignSlave(name, webSocket, semaphoreSlim);
                        lock (signSlaveList)
                        {
                            signSlaveList.Add(signSlave);
                        }

                        while (webSocket.State == WebSocketState.Open)
                        {
                            if (await signSlave.SemaphoreSlim.WaitAsync(0))
                            {
                                try
                                {
                                    var headLength = 10000;
                                    var memory = arraySegment.AsMemory(0, sizeof(long) + headLength);
                                    memory.Span.Clear();
                                    BinaryPrimitives.WriteInt64LittleEndian(memory.Span, headLength);
                                    MessageType messageType = new MessageType()
                                    {
                                        Type = 1
                                    };

                                    MemoryMarshal.Write(memory.Span.Slice(sizeof(long)), in messageType);

                                    await webSocket.SendAsync(memory, WebSocketMessageType.Binary,
                                         WebSocketMessageFlags.EndOfMessage, CancellationToken.None);
                                }
                                finally
                                {
                                    signSlave.SemaphoreSlim.Release();
                                }
                            }

                            await Task.Delay(TimeSpan.FromMilliseconds(100));
                        }

                        lock (signSlaveList)
                        {
                            signSlaveList.Remove(signSlave);
                        }
                        signSlave.Dispose();
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

            app.Map("/sign", async (Microsoft.AspNetCore.Http.HttpContext content) =>
            {
                // Get a free slave
                SignSlave? freeSlave = null;
                lock (signSlaveList)
                {
                    foreach (var slave in signSlaveList)
                    {
                        if (slave.SemaphoreSlim.Wait(0))
                        {
                            freeSlave = slave;
                            break;
                        }
                    }
                }

                if (freeSlave == null)
                {
                    lock (signSlaveList)
                    {
                        if (signSlaveList.Count == 0)
                        {
                        }
                        else
                        {
                            freeSlave = signSlaveList[Random.Shared.Next(signSlaveList.Count)];
                        }
                    }

                    if (freeSlave == null)
                    {
                        content.Response.StatusCode = 503;
                        await content.Response.StartAsync();
                        await content.Response.CompleteAsync();
                        return;
                    }

                    await freeSlave.SemaphoreSlim.WaitAsync();
                }

                content.Response.StatusCode = 200;
                await content.Response.StartAsync();

                var buffer = ArrayPool<byte>.Shared.Rent(10240);
                try
                {
                    // Read content to byte array

                    Stream requestContentStream = content.Request.Body;

                    // Header Length
                    // 默认 C# 就是小端
                    Memory<byte> memory = buffer.AsMemory();

                    int headerLength = 100;
                    BinaryPrimitives.WriteInt64LittleEndian(memory.Span, headerLength);
                    memory.Span.Slice(sizeof(long), headerLength).Clear();
                    var webSocket = freeSlave.WebSocket;
                    await webSocket.SendAsync(memory[..(headerLength + sizeof(long))],
                        WebSocketMessageType.Binary, WebSocketMessageFlags.None, CancellationToken.None);

                    while (true)
                    {
                        var readCount = await requestContentStream.ReadAsync(memory);
                        if (readCount == 0)
                        {
                            break;
                        }

                        await webSocket.SendAsync(memory.Slice(0, readCount), WebSocketMessageType.Binary,
                            WebSocketMessageFlags.None, CancellationToken.None);
                    }

                    await webSocket.SendAsync([], WebSocketMessageType.Binary, endOfMessage: true, CancellationToken.None);

                    while (true)
                    {
                        var receiveResult = await webSocket.ReceiveAsync(memory, CancellationToken.None);

                        await content.Response.BodyWriter.WriteAsync(memory.Slice(0, receiveResult.Count));

                        if (receiveResult.EndOfMessage)
                        {
                            break;
                        }
                    }
                }
                catch
                {
                }
                finally
                {
                    freeSlave.SemaphoreSlim.Release();
                    ArrayPool<byte>.Shared.Return(buffer);
                    await content.Response.CompleteAsync();
                }
            });

            app.Run();
        }
    }
}

record SignSlave(string Name, WebSocket WebSocket, SemaphoreSlim SemaphoreSlim) : IDisposable
{
    public void Dispose()
    {
        SemaphoreSlim.Dispose();
    }
}