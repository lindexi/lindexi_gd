// See https://aka.ms/new-console-template for more information

using System.Text.Json.Nodes;

var httpClient = new HttpClient();

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