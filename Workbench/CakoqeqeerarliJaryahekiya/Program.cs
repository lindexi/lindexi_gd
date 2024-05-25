// See https://aka.ms/new-console-template for more information

using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Web;

var httpClient = new HttpClient();

var youDaoOfficialApiService = new YouDaoOfficialApiService(httpClient);

var result = await youDaoOfficialApiService.TranslateAsync("我是一名教师");

var word = "example";
var url =
    $"https://dict.youdao.com/jsonapi?xmlVersion=5.1&client=mobile&q={word}&dicts=&keyfrom=&model=&mid=&imei=&vendor=&screen=&ssid=&network=5g&abtest=&jsonversion=2";

//var text = await httpClient.GetStringAsync(url);
var text = File.ReadAllText("YouDao.txt");
var jsonNode = JsonObject.Parse(text);

var synoList = GetSynoList(jsonNode);
var discriminateList = GetDiscriminateList(jsonNode);


Console.WriteLine("Hello, World!");




List<string> GetDiscriminateList(JsonNode? jsonNode)
{
    var discriminateList = new List<string>();

    var discriminate = jsonNode?["discriminate"];

    var data = discriminate?["data"] as JsonArray;

    if (data is null)
    {
        return discriminateList;
    }

    foreach (var source in data)
    {
        var usages = source?["usages"] as JsonArray;
        if (usages is null)
        {
            continue;
        }

        foreach (var usage in usages)
        {
            var headword = usage?["headword"];
            if (headword is null)
            {
                continue;
            }

            discriminateList.Add(headword.ToString());
        }
    }

    return discriminateList;
}

List<string> GetSynoList(JsonNode? jsonNode)
{
    var synoList = new List<string>();

    var synoRoot = jsonNode?["syno"];
    if (synoRoot == null)
    {
        return synoList;
    }

    var synos = synoRoot["synos"];
    if (synos is JsonArray synoJsonArray)
    {
        foreach (var synoNode in synoJsonArray)
        {
            var subSyno = synoNode?["syno"];
            var words = subSyno?["ws"] as JsonArray;

            if (words == null)
            {
                continue;
            }

            foreach (var wordValue in words)
            {
                var value = wordValue?["w"];
                if (value == null)
                {
                    continue;
                }

                synoList.Add(value.ToString());
            }
        }
    }

    return synoList;
}

/// <summary>
/// 有道官方API
/// </summary>
public class YouDaoOfficialApiService
{
    public YouDaoOfficialApiService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
    }

    private readonly HttpClient _httpClient;

    public async Task<string?> TranslateAsync(string text)
    {
        var queryText = text;
        var requestUrl = GetRequestUrl(queryText);
        var httpResponseMessage = await _httpClient.GetAsync(requestUrl);
        httpResponseMessage.EnsureSuccessStatusCode();

        var translateResult = await httpResponseMessage.Content.ReadAsStringAsync();
        var rootNode = JsonNode.Parse(translateResult);
        var translationJsonArray = rootNode?["translation"] as JsonArray;
        return translationJsonArray?.FirstOrDefault()?.ToString();
    }

    private static string GetRequestUrl(string queryText)
    {
        string salt = DateTime.Now.Millisecond.ToString();

        using MD5 md5 = MD5.Create();
        string md5Str = _appKey + queryText + salt + _appSecret;
        byte[] output = md5.ComputeHash(Encoding.UTF8.GetBytes(md5Str));
        string sign = BitConverter.ToString(output).Replace("-", "");

        var queryTextDecode = HttpUtility.UrlDecode(queryText, Encoding.GetEncoding("UTF-8"));

        var requestUrl = string.Format(
            "http://openapi.youdao.com/api?appKey={0}&q={1}&from={2}&to={3}&sign={4}&salt={5}",
            _appKey,
            queryTextDecode,
            _from, _to, sign, salt);

        return requestUrl;
    }

    const string _appKey = "17244a88182153cf";
    const string _from = "auto";
    const string _to = "zhs";
    const string _appSecret = "on7de03hB5JhqpJqXCNGkaomq4PukQ62";
}