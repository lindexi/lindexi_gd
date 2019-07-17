using MimeKit;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Toolkit.Parsers.Rss;
using Newtonsoft.Json;

namespace CinerbarhelKellonajel
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var mailKey = GetMailKey();

            await ReadRssAsync();

            //await SendSubscription(mailKey);
        }

        private static async Task SendSubscription(MailKey mailKey,string title,string context,string mail)
        {
            var messageToSend = new MimeMessage
            {
                Sender = new MailboxAddress("lindexi", "lindexi_gd@outlook.com"),
                Subject = title,
                Body = new TextPart(MimeKit.Text.TextFormat.Html)
                {
                    Text = context,
                },
                To =
                {
                    new MailboxAddress(mail)
                }
            };

            using (var smtp = new MailKit.Net.Smtp.SmtpClient())
            {
                await smtp.ConnectAsync("smtp-mail.outlook.com", port: 587, MailKit.Security.SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(mailKey.Name, mailKey.Key);
                await smtp.SendAsync(messageToSend);
                await smtp.DisconnectAsync(true);
            }
        }

        private static async Task ReadRssAsync()
        {
            var str = await GetRss();

            var parser = new RssParser(){};

            var rssSchemata = parser.Parse(str);
            
            foreach (var temp in rssSchemata)
            {
                
            }
        }

        private static async Task<string> GetRss()
        {
            var file = new FileInfo(@"D:\lindexi\rss");

            if (file.Exists)
            {
                return File.ReadAllText(file.FullName);
            }

            var http = new HttpClient();
            {

            }

            var userAgent = http.DefaultRequestHeaders.UserAgent;
            userAgent.Clear();
            userAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/60.0.3112.113 Safari/537.36");

            var str = await http.GetStringAsync("http://feed.cnblogs.com/blog/sitecateogry/108698/rss");

            File.WriteAllText(file.FullName,str);

            return str;
        }

        private static MailKey GetMailKey()
        {
            var mailKey = new FileInfo("D:\\lindexi\\mailkey");
            if (mailKey.Exists)
            {
                using (var stream = mailKey.OpenText())
                {
                    var str = stream.ReadToEnd();
                    return JsonConvert.DeserializeObject<MailKey>(str);
                }
            }

            return null;
        }
    }
}
