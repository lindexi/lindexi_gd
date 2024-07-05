// See https://aka.ms/new-console-template for more information

using System.Text.Encodings.Web;

var token = "6cecf0cf8d508f0afa762774e2dd310a-c-app";

//string json =
//        """
//        {
//          "trace": "edd5df80-df7f-4acf-8f67-68fd2f096426",
//          "data": 
//          {
//            "code": "000651.SZ",
//            "kline_type": 1, 
//            "kline_timestamp_end": 0,
//             "query_kline_num": 2, 
//             "adjust_type": 0
//          }
//        }
//        """;

//var queryData = Uri.EscapeDataString(json);
//var url = $"https://quote.tradeswitcher.com/quote-stock-b-api/kline?token={token}&query={queryData}";

//using var httpClient = new HttpClient();
//var response = await httpClient.GetStringAsync(url);
//// {"ret":200,"msg":"ok","trace":"edd5df80-df7f-4acf-8f67-68fd2f096426","data":{"code":"000055.SZ","kline_type":1,"kline_list":[{"timestamp":"1720162560","open_price":"3.480000","close_price":"3.480000","high_price":"3.480000","low_price":"3.470000","volume":"147","turnover":"51110.000000"},{"timestamp":"1720162800","open_price":"3.480000","close_price":"3.480000","high_price":"3.480000","low_price":"3.480000","volume":"141","turnover":"49068.000000"}]}}



//string json =
//    """
//    {
//      "trace": "12d5df80-df7f-4acf-8f67-68fd2f096426",
//      "data":
//      {
//        "symbol_list": [{"code": "000651.SZ"}]
//      }
//    }
//    """;

//var queryData = Uri.EscapeDataString(json);
//var url = $"https://quote.tradeswitcher.com/quote-stock-b-api/depth-tick?token={token}&query={queryData}";

//using var httpClient = new HttpClient();
//var response = await httpClient.GetStringAsync(url);
//// {"ret":200,"msg":"ok","trace":"12d5df80-df7f-4acf-8f67-68fd2f096426","data":{"tick_list":[{"code":"000651.SZ","seq":"1836410","tick_time":"1720162800891","bids":[{"price":"37.700000","volume":"460"},{"price":"37.690000","volume":"69"},{"price":"37.680000","volume":"290"},{"price":"37.670000","volume":"29"},{"price":"37.660000","volume":"119"}],"asks":[{"price":"37.710000","volume":"32"},{"price":"37.720000","volume":"125"},{"price":"37.730000","volume":"83"},{"price":"37.740000","volume":"38"},{"price":"37.750000","volume":"17"}]}]}}


string json =
    """
    {
      "trace": "12d5df80-df7f-4acf-8f67-68fd2f096426",
      "data":
      {
        "symbol_list": [{"code": "000651.SZ"}]
      }
    }
    """;

var queryData = Uri.EscapeDataString(json);
var url = $"https://quote.tradeswitcher.com/quote-stock-b-api/trade-tick?token={token}&query={queryData}";

using var httpClient = new HttpClient();
var response = await httpClient.GetStringAsync(url);
// {"ret":200,"msg":"ok","trace":"12d5df80-df7f-4acf-8f67-68fd2f096426","data":{"tick_list":[{"code":"000651.SZ","seq":"1836409","tick_time":"1720162800891","price":"37.700000","volume":"2567","turnover":"96775.900000","trade_direction":1}]}}

Console.WriteLine("Hello, World!");
