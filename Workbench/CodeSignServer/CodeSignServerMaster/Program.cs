using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using CodeSignServerMaster.Contexts;
using Microsoft.AspNetCore.Connections;

namespace CodeSignServerMaster
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            builder.WebHost.UseKestrel(options =>
            {
                // 无限制请求体大小
                options.Limits.MaxRequestBodySize = null;
            });

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
                            bool beBreak = false;
                            if (await signSlave.SemaphoreSlim.WaitAsync(0))
                            {
                                try
                                {
                                    MessageType messageType = new MessageType()
                                    {
                                        Type = 1
                                    };

                                    var memory = arraySegment.AsMemory(0, messageType.HeadLength);
                                    memory.Span.Clear();

                                    MemoryMarshal.Write(memory.Span, in messageType);

                                    await webSocket.SendAsync(memory, WebSocketMessageType.Binary,
                                         WebSocketMessageFlags.EndOfMessage, CancellationToken.None);

                                    using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                                    try
                                    {
                                        ValueWebSocketReceiveResult result2 =
                                            await webSocket.ReceiveAsync(memory, cancellationTokenSource.Token);
                                        var responseMessageType = MemoryMarshal.Read<MessageType>(memory.Span.Slice(0, result2.Count));
                                        Debug.Assert(responseMessageType.Type == 3);
                                    }
                                    catch (OperationCanceledException e)
                                    {
                                        Console.WriteLine(e);
                                        beBreak = true;
                                        break;
                                    }
                                    catch (WebSocketException e)
                                    {
                                        if (e.InnerException is ConnectionResetException resetException)
                                        {
                                            Debug.Assert((uint) resetException.HResult == 0x80131620);
                                        beBreak = true;
                                            break;
                                        }
                                    }
                                }
                                finally
                                {
                                    if (beBreak)
                                    {
                                        lock (signSlaveList)
                                        {
                                            signSlaveList.Remove(signSlave);
                                        }
                                    }

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
                var contentLength = content.Request.Headers.ContentLength;
                if(contentLength == null || contentLength <= 0)
                {
                    await FastFail(StatusCodes.Status411LengthRequired, "请求体不能为空");
                    return;
                }

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
                        await FastFail(StatusCodes.Status503ServiceUnavailable, $"无可用签名服务器");
                        return;
                    }

                    await freeSlave.SemaphoreSlim.WaitAsync();
                }

                content.Response.StatusCode = 200;
                await content.Response.StartAsync();

                var buffer = ArrayPool<byte>.Shared.Rent(102400);
                try
                {
                    // Read content to byte array

                    Stream requestContentStream = content.Request.Body;

                    // Header Length
                    // 默认 C# 就是小端
                    var messageType = new MessageType()
                    {
                        Type = 2,
                    };
                    Memory<byte> memory = buffer.AsMemory(0, messageType.HeadLength);
                    memory.Span.Clear();
                    MemoryMarshal.Write(memory.Span, in messageType);
                 
                    var webSocket = freeSlave.WebSocket;
                    await webSocket.SendAsync(memory,
                        WebSocketMessageType.Binary, WebSocketMessageFlags.EndOfMessage, CancellationToken.None);

                    var stopwatch = Stopwatch.StartNew();
                    long totalReadCount = 0;
                    while (true)
                    {
                        // 转发请求的内容
                        memory = buffer.AsMemory();
                        var readCount = await requestContentStream.ReadAsync(memory);
                        if (readCount == 0)
                        {
                            break;
                        }

                        await webSocket.SendAsync(memory.Slice(0, readCount), WebSocketMessageType.Binary,
                            WebSocketMessageFlags.None, CancellationToken.None);

                        totalReadCount += readCount;
                        var speed = totalReadCount / stopwatch.Elapsed.TotalSeconds;
                    }

                    // System.ArgumentNullException:“Value cannot be null. (Parameter 'buffer.Array')”
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

                return;

                async Task FastFail(int statusCode , string errorMessage)
                {
                    if (contentLength>0)
                    {
                        // 读取请求体，避免客户端异常

                        // 不能通过 CompleteAsync 设置读取完成，此时会让客户端无法完成传输
                        //await content.Request.BodyReader.CompleteAsync();
                        using var emptyStream = new EmptyStream();
                        await content.Request.Body.CopyToAsync(emptyStream);
                    }

                    content.Response.StatusCode = statusCode;
                    await content.Response.StartAsync();
                    await content.Response.WriteAsync(errorMessage);
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