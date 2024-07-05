// See https://aka.ms/new-console-template for more information

using System.Text.Encodings.Web;

var token = "6cecf0cf8d508f0afa762774e2dd310a-c-app";

string json =
        """
        {
          "trace": "edd5df80-df7f-4acf-8f67-68fd2f096426",
          "data": 
          {
            "code": "700.HK",
            "kline_type": 1, 
            "kline_timestamp_end": 0,
             "query_kline_num": 2, 
             "adjust_type": 0
          }
        }
        """;

var queryData = Uri.EscapeDataString(json);
var url = $"https://quote.tradeswitcher.com/quote-stock-b-api/kline?token={token}&query={queryData}";

using var httpClient = new HttpClient();
var response = await httpClient.GetStringAsync(url);
// {"ret":200,"msg":"ok","trace":"edd5df80-df7f-4acf-8f67-68fd2f096426","data":{"code":"700.HK","kline_type":1,"kline_list":[{"timestamp":"1720166340","open_price":"380.400000","close_price":"380.400000","high_price":"380.400000","low_price":"380.000000","volume":"99200","turnover":"37729070.000000"},{"timestamp":"1720166400","open_price":"379.800000","close_price":"379.800000","high_price":"379.800000","low_price":"379.800000","volume":"1203200","turnover":"456975360.000000"}]}}
Console.WriteLine("Hello, World!");
