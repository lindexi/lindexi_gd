// See https://aka.ms/new-console-template for more information

var httpClient = new HttpClient();

var word = "example";
var url =
    $"https://dict.youdao.com/jsonapi?xmlVersion=5.1&client=mobile&q={word}&dicts=&keyfrom=&model=&mid=&imei=&vendor=&screen=&ssid=&network=5g&abtest=&jsonversion=2";

var text = await httpClient.GetStringAsync(url);


Console.WriteLine("Hello, World!");
