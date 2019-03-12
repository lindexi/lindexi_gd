using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace lindexi.src
{
    /// <summary>
    /// QPush 快推 从电脑到手机最方便的文字推送工具
    /// </summary>
    public class Qpush
    {
        public Qpush(string name, string code)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (code == null) throw new ArgumentNullException(nameof(code));

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name 不能为空");
            }

            if (string.IsNullOrEmpty(code))
            {
                throw new ArgumentNullException("code 不能为空");
            }

            Name = name;
            Code = code;
        }

        /// <summary>
        /// 推名
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 推码
        /// </summary>
        public string Code { get; }

        /// <summary>
        /// 推送信息
        /// </summary>
        public async Task<string> PushMessageAsync(string str)
        {
            const string url = "https://qpush.me/pusher/push_site/";

            var httpClient = new HttpClient();

            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/71.0.3578.98 Safari/537.36");

            HttpContent content =
                new StringContent(
                    $"name={Uri.EscapeUriString(Name)}&code={Uri.EscapeUriString(Code)}&sig=&cache=false&msg%5Btext%5D={Uri.EscapeUriString(str)}",
                    Encoding.UTF8, "application/x-www-form-urlencoded");
            var code = await (await httpClient.PostAsync(url, content)).Content.ReadAsStringAsync();

            return code;
        }
    }
}