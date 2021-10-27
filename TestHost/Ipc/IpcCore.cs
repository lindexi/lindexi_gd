using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using dotnetCampus.Ipc.Abstractions;
using dotnetCampus.Ipc.PipeCore;
using dotnetCampus.Ipc.PipeCore.Context;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.TestHost.Ipc
{
    public class IpcCore
    {
        public IpcCore(IServiceProvider serviceProvider)
        {
            IpcServer = new IpcProvider(IpcServerName, new IpcConfiguration()
            {
                DefaultIpcRequestHandler = new DelegateIpcRequestHandler(async context =>
                {
                    var server = (TestServer)serviceProvider.GetRequiredService<IServer>();

                    var requestMessage = HttpMessageSerializer.DeserializeToRequest(context.IpcBufferMessage.Buffer);

                    var clientHandler = (ClientHandler) server.CreateHandler();
                    var response = await clientHandler.SendInnerAsync(requestMessage, CancellationToken.None);

                    var responseByteList = HttpMessageSerializer.Serialize(response);

                    return new IpcHandleRequestMessageResult(new IpcRequestMessage($"[Response][{requestMessage.Method}] {requestMessage.RequestUri}", responseByteList));
                })
            });
           
        }

        public void Start() => IpcServer.StartServer();
        public IpcProvider IpcServer { set; get; }

        public static readonly string IpcServerName = Guid.NewGuid().ToString("N");


        public static PeerProxy Client { set; get; }

        public static IpcProvider? IpcClient { set; get; }

        public static async Task<HttpClient> GetHttpClientAsync()
        {
            if (IpcClient == null)
            {
                IpcClient = new IpcProvider();
                IpcClient.StartServer();
                var peer = await IpcClient.GetAndConnectToPeerAsync(IpcServerName);
                Client = peer;
            }

            return new HttpClient(new IpcNamedPipeClientHandler(Client))
            {
                BaseAddress = BaseAddress,
            };
        }

        public static Uri BaseAddress { get; set; } = new Uri("http://localhost/");
    }

    public class IpcNamedPipeClientHandler : HttpMessageHandler
    {
       

        public IpcNamedPipeClientHandler(PeerProxy client)
        {
            Client = client;
        }

        public PeerProxy Client { get; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var message = HttpMessageSerializer.Serialize(request);

            var response = await Client.GetResponseAsync(new IpcRequestMessage(request.RequestUri?.ToString() ?? request.Method.ToString(),
                 message));

            return HttpMessageSerializer.DeserializeToResponse(response.Buffer);
        }
    }

    public class HttpRequestMessageSerializeContent : HttpRequestMessageContentBase
    {
        public HttpRequestMessageSerializeContent(HttpRequestMessage message) : base(message)
        {
            Headers = message.Headers;
        }

        public HttpRequestHeaders Headers { get; set; }
    }

    public class HttpRequestMessageDeserializeContent : HttpRequestMessageContentBase
    {
        public JContainer Headers { set; get; }

        public HttpRequestMessage ToHttpResponseMessage()
        {
            var result = new HttpRequestMessage()
            {
                Version = Version,
                VersionPolicy = VersionPolicy,
                Method = Method,
                RequestUri = RequestUri,

            };

            var memoryStream = new MemoryStream(Convert.FromBase64String(ContentBase64));
            var text = Encoding.UTF8.GetString(memoryStream.ToArray());
            var streamContent = new StreamContent(memoryStream);
            result.Content = streamContent;

            return result;
        }
    }

    public class HttpRequestMessageContentBase
    {
        public HttpRequestMessageContentBase(HttpRequestMessage message)
        {
            Version = message.Version;
            VersionPolicy = message.VersionPolicy;
            Method = message.Method;
            RequestUri = message.RequestUri;
            Options = message.Options;

            if (message.Content != null)
            {
                using var memoryStream = new MemoryStream();
                message.Content.CopyTo(memoryStream, null, CancellationToken.None);
                ContentBase64 = Convert.ToBase64String(memoryStream.ToArray());
            }
        }

        public HttpRequestMessageContentBase()
        {
        }

        public string ContentBase64 { set; get; }

        public Version Version { set; get; }
        public HttpVersionPolicy VersionPolicy { set; get; }

        public HttpMethod Method { set; get; }
        public Uri? RequestUri { set; get; }
        public HttpRequestOptions Options { set; get; }


    }

    public static class HttpMessageSerializer
    {
        public static byte[] Serialize(HttpResponseMessage response)
        {
            throw null;
        }

        public static byte[] Serialize(HttpRequestMessage request)
        {
            var json = JsonConvert.SerializeObject(new HttpRequestMessageSerializeContent(request));

            return Encoding.UTF8.GetBytes(json);
        }

        public static HttpResponseMessage DeserializeToResponse(byte[] d)
        {
            throw null;
        }

        public static HttpRequestMessage DeserializeToRequest(byte[] d)
        {
            var json = Encoding.UTF8.GetString(d);
            var httpRequestMessageDeserializeContent = JsonConvert.DeserializeObject<HttpRequestMessageDeserializeContent>(json);
            return httpRequestMessageDeserializeContent.ToHttpResponseMessage();
        }
    }

    class IpcHandleRequestMessageResult : IIpcHandleRequestMessageResult
    {
        [DebuggerStepThrough]
        public IpcHandleRequestMessageResult(IpcRequestMessage returnMessage)
        {
            ReturnMessage = returnMessage;
        }

        public IpcRequestMessage ReturnMessage { get; }
    }
}
