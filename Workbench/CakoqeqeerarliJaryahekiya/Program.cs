// See https://aka.ms/new-console-template for more information

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Web;

var inputText = "我是一名教师";

var gapFillingActivityTranslationProvider = new GapFillingActivityTranslationProvider();
var result = await gapFillingActivityTranslationProvider.Build(inputText);

if (result.Success)
{
    var root = new JsonObject();
    root["activityType"] = "GapFilling";

    var activityData = new JsonObject();
    root["activityData"] = activityData;

    var paragraphs = new JsonArray();
    var invalids = new JsonArray();

    activityData["paragraphs"] = paragraphs;
    activityData["invalids"] = invalids;

    // 第一段，原文
    var paragraph1 = JsonObject.Parse($$"""
                                      {
                                                     "stemContents": 
                                                     [{
                                                         "text": "{{result.InputText}}",
                                                         "filling": false
                                                     }]
                                      }
                                      """);
    paragraphs.Add(paragraph1);

    var paragraph2 = new JsonObject();
    paragraphs.Add(paragraph2);

    var stemContents = new JsonArray();
    paragraph2["stemContents"] = stemContents;
    foreach (var fill in result.Filling)
    {
        stemContents.Add(JsonNode.Parse($$"""
                                        {
                                                           "text": "{{fill}}",
                                                           "filling": true
                                        }
                                        """));
    }

    foreach (var invalid in result.Invalids)
    {
        invalids.Add(invalid);
    }

    var jsonString = root.ToJsonString();
}

Console.WriteLine("Hello, World!");

/// <summary>
/// 选词填空出题的提供器
/// </summary>
/// 输入中文，给出选词填空，辅助干扰项
public class GapFillingActivityTranslationProvider
{
    public async Task<GapFillingActivityTranslationResult> Build(string inputText)
    {
        if (inputText.Length > 20)
        {
            return GapFillingActivityTranslationResult.Fail("输入长度过长");
        }

        using var httpClient = new HttpClient();

        var youDaoOfficialApiService = new YouDaoOfficialApiService(httpClient);

        var result = await youDaoOfficialApiService.TranslateAsync(inputText);

        if (result == null)
        {
            return GapFillingActivityTranslationResult.Fail("翻译失败");
        }

        // 分词
        var fillingList = result.Split(' ').ToList();
        if (fillingList.Count > 10)
        {
            return GapFillingActivityTranslationResult.Fail("翻译的英语单词过多");
        }

        //// 干扰项数量
        //var invalidsCount = fillingList.Count / 3;
        var invalidList = new List<string>();
        foreach (var word in fillingList)
        {
            var invalid = TryGetInvalids(word)?.ToList();

            if (invalid is null)
            {
                var (synoList, discriminateList) = await youDaoOfficialApiService.GetSynoDiscriminateList(word);

                invalid = synoList.Concat(discriminateList).Distinct().ToList();
            }

            if (invalid.Count > 0)
            {
                foreach (var fill in fillingList)
                {
                    // 不能和答案相同
                    invalid.Remove(fill);
                }

                if (invalid.Count > 0)
                {
                    var invalidWord = invalid[Random.Shared.Next(invalid.Count)];
                    invalidList.Add(invalidWord);
                }
            }
        }

        return new GapFillingActivityTranslationResult(inputText, fillingList, invalidList);
    }

    /// <summary>
    /// 默认干扰项字典
    /// </summary>
    private string[][] DefaultInvalids { get; }
        =
        [
            ["you", "me", "my","I","he","she","it"],
            ["a", "an",],
            ["was","are","am","is"]
        ];

    private string[]? TryGetInvalids(string word)
    {
        foreach (var invalidList in DefaultInvalids)
        {
            if (invalidList.Contains(word))
            {
                return invalidList;
            }
        }

        return null;
    }
}

public readonly record struct GapFillingActivityTranslationResult(
    string InputText,
    List<string> Filling,
    List<string> Invalids)
{
    public bool Success => Filling != null!;

    public string? ErrorMessage { get; init; }

    public static GapFillingActivityTranslationResult Fail(string errorMessage)
    {
        GapFillingActivityTranslationResult result = default;

        return result with
        {
            ErrorMessage = errorMessage
        };
    }
};

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

    public async Task<(List<string> synoList, List<string> discriminateList)> GetSynoDiscriminateList(string word)
    {
        var url =
            $"https://dict.youdao.com/jsonapi?xmlVersion=5.1&client=mobile&q={word}&dicts=&keyfrom=&model=&mid=&imei=&vendor=&screen=&ssid=&network=5g&abtest=&jsonversion=2";

        var httpClient = _httpClient;

        var text = await httpClient.GetStringAsync(url);
        //var text = File.ReadAllText("YouDao.txt");
        var jsonNode = JsonObject.Parse(text);

        var synoList = GetSynoList(jsonNode);
        var discriminateList = GetDiscriminateList(jsonNode);

        return (synoList, discriminateList);
    }

    private List<string> GetDiscriminateList(JsonNode? jsonNode)
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

    private List<string> GetSynoList(JsonNode? jsonNode)
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