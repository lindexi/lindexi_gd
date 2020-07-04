using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace NalbaberejarlereheaFewhewerhichee
{
    // https://github.com/basespace/TerminalVelocity
    // https://github.com/golavr/SegmentDownload
    //

    class Program
    {
        static async Task Main(string[] args)
        {
            var url = "http://speedtest-ny.turnkeyinternet.net/100mb.bin";

            var httpClient = new HttpClient();

            try
            {
                var message = await httpClient.GetAsync(url);

                if (message.StatusCode == HttpStatusCode.OK)
                {
                    if (message.Version >= new Version(1, 1))
                    {
                        var headersContentLength = message.Content.Headers.ContentLength;

                        var downloadLength = 100;
                        var endLength = headersContentLength - downloadLength;
                        var client = new HttpClient();
                        client.DefaultRequestHeaders.Range = new RangeHeaderValue(endLength, null);

                        var httpResponseMessage = await client.GetAsync(url);
                        var contentLength = httpResponseMessage.Content.Headers.ContentLength;

                        if (contentLength == downloadLength)
                        {

                        }
                    }
                }
            }
            catch (Exception e)
            {
            }
        }
    }
}
