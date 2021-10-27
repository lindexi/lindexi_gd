using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using dotnetCampus.Ipc.Abstractions;
using dotnetCampus.Ipc.PipeCore;
using dotnetCampus.Ipc.PipeCore.Context;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.TestHost.Ipc
{
    public static class IpcCore
    {
        public static async Task Build()
        {
            var ipcProvider = new IpcProvider(Guid.NewGuid().ToString("N"), new IpcConfiguration()
            {
                DefaultIpcRequestHandler = new DelegateIpcRequestHandler(async context =>
                {


                    return new IpcHandleRequestMessageResult(new IpcRequestMessage("123", new byte[0]));
                })
            });
            ipcProvider.StartServer();
            IpcServer = ipcProvider;
            IpcClient = new IpcProvider();
            IpcClient.StartServer();
            var peer = await IpcClient.GetAndConnectToPeerAsync(ipcProvider.IpcServerService.IpcContext.PipeName);
            Client = peer;
        }



        public static PeerProxy Client { set; get; }

        public static IpcProvider IpcServer { set; get; }
        public static IpcProvider IpcClient { set; get; }

    }

    public class IpcNamedPipeClientHandler : HttpMessageHandler
    {
        public IpcNamedPipeClientHandler()
        {
            Client = IpcCore.Client;
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

    public class HttpRequestMessageSerializeContent: HttpRequestMessageContentBase
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

            foreach (var httpRequestOption in Options)
            {
                result.Options.Set<string>(new HttpRequestOptionsKey<string>(httpRequestOption.Key),
                    httpRequestOption.Value?.ToString());
            }

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

            if (message.Content is JsonContent jsonContent)
            {
                ContentJson = JsonConvert.SerializeObject(jsonContent);
            }
        }

        public HttpRequestMessageContentBase()
        {
        }

        public string ContentJson { set; get; }

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
            var r = JsonConvert.DeserializeObject<HttpRequestMessageDeserializeContent>(json);
            if (request.Content is JsonContent jsonContent)
            {

            }



            return new byte[0];
        }

        public static HttpResponseMessage DeserializeToResponse(byte[] d)
        {
            throw null;
        }

        public static HttpRequestMessage DeserializeToRequest(byte[] d)
        {
            throw null;
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
