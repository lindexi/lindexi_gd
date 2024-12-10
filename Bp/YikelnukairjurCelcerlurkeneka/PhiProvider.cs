using System.Net;
using System.Text.Json;
using System.Text;

namespace YikelnukairjurCelcerlurkeneka;

record ChatRequest(string Prompt);

public class PhiProvider
{
    public static PhiProvider GetPhiProvider() => new PhiProvider();

    public async Task<PhiResponse> ChatAsync(string prompt)
    {
        using var httpClient = new HttpClient();
        var url = "http://172.20.114.91:5017";

        var request = new ChatRequest(prompt);
        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var httpRequestMessage = new HttpRequestMessage();
        httpRequestMessage.RequestUri = new Uri($"{url}/Chat");
        httpRequestMessage.Method = HttpMethod.Post;
        httpRequestMessage.Content = content;

        try
        {
            var response = await httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead);
            if (response.StatusCode == HttpStatusCode.InternalServerError)
            {
                string? errorMessage = null;
                try
                {
                    errorMessage = await response.Content.ReadAsStringAsync();
                }
                catch (Exception e)
                {
                    // 失败就失败
                }

                return new PhiResponse()
                {
                    Success = false,
                    ErrorMessage = errorMessage,
                    ResponseStream = Stream.Null
                };
            }

            var stream = await response.Content.ReadAsStreamAsync();
            return new PhiResponse()
            {
                Success = true,
                ErrorMessage = null,
                ResponseStream = stream
            };
        }
        catch (Exception e)
        {
            return new PhiResponse()
            {
                Success = false,
                ErrorMessage = e.Message,
                ResponseStream = Stream.Null
            };
        }
    }
}