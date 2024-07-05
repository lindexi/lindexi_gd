// See https://aka.ms/new-console-template for more information

using System.Text;
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
//var url = $"https://quote.tradeswitcher.com/quote-stock-b-api/trade-tick?token={token}&query={queryData}";

//using var httpClient = new HttpClient();
//var response = await httpClient.GetStringAsync(url);
//// {"ret":200,"msg":"ok","trace":"12d5df80-df7f-4acf-8f67-68fd2f096426","data":{"tick_list":[{"code":"000651.SZ","seq":"1836409","tick_time":"1720162800891","price":"37.700000","volume":"2567","turnover":"96775.900000","trade_direction":1}]}}

//// 一天只 50 次查询
//// [最新最全的免费股票数据接口--沪深A股实时交易数据API接口（一）_股票数据接口api-CSDN博客](https://blog.csdn.net/u012940698/article/details/126777239)
//response = await httpClient.GetStringAsync("http://api.mairui.club/hsrl/zbjy/000651/3579ff677319c83cc6");

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

using var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Referer", "https://finance.sina.com.cn/");

var response = await httpClient.GetAsync("https://hq.sinajs.cn/list=sz000651");
var stream = await response.Content.ReadAsStreamAsync();
var streamReader = new StreamReader(stream, Encoding.GetEncoding("GBK"));
var text = await streamReader.ReadToEndAsync();

// 数据依次是“股票名称、今日开盘价、昨日收盘价、当前价格、今日最高价、今日最低价、竞买价、竞卖价、成交股数、成交金额、买1手、买1报价、买2手、买2报价、…、买5报价、…、卖5报价、日期、时间”。
// var hq_str_sz000651="格力电器,38.010,38.020,37.700,38.240,37.200,37.700,37.710,39027322,1466749485.590,46000,37.700,6900,37.690,29000,37.680,2900,37.670,11900,37.660,3200,37.710,12500,37.720,8300,37.730,3800,37.740,1700,37.750,2024-07-05,15:00:00,00";


Console.WriteLine("Hello, World!");
