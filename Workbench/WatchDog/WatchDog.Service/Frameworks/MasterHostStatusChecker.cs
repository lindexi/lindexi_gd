namespace WatchDog.Service.Frameworks;

public class MasterHostStatusChecker : IMasterHostStatusChecker
{
    public MasterHostStatusChecker(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    private readonly IHttpClientFactory _httpClientFactory;

    public async Task<bool> CheckMasterHostEnableAsync(string host)
    {
        var httpClient = _httpClientFactory.CreateClient();
        if (host.StartsWith("Fake-"))
        {
            return true;
        }

        try
        {
            // 这里可以使用 HttpClient 去请求一下，看看是否可用
            var url = $"http://{host}/Dog/Enable";
            var result = await httpClient.GetStringAsync(url);
            _ = result;
            // 不用判断返回值，只要能访问就可以
            return true;
        }
        catch (Exception e)
        {
            return false;
        }
    }
}