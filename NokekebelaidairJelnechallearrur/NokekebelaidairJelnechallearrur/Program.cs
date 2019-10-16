using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.SyndicationFeed;
using Microsoft.SyndicationFeed.Atom;
using Microsoft.SyndicationFeed.Rss;
using SimpleFeedNS;

namespace NokekebelaidairJelnechallearrur
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Dictionary<string, DateTime> publishBlogList = new Dictionary<string, DateTime>();

            while (true)
            {
                try
                {
                    Console.WriteLine($"{DateTime.Now} 开始拉取博客");

                    List<string> feedList = new List<string>()
                    {
                        "https://lindexi.gitee.io/feed.xml",
                        "http://feed.cnblogs.com/blog/u/148394/rss/",
                    };

                    var file = "blog.txt";
                    var matterMostConfigFile = "MatterMost.txt";

                    if (File.Exists(file))
                    {
                        feedList = File.ReadAllLines(file).ToList();
                    }
                    else
                    {
                        File.WriteAllLines(file, feedList);
                    }

                    var minTime = TimeSpan.FromDays(1);

                    var matterMostUrl = "http://127.0.0.1:8065/hooks/357owee7f7rumn316mcdrmmiko";

                    if (File.Exists(matterMostConfigFile))
                    {
                        matterMostUrl = File.ReadAllText(matterMostConfigFile);
                    }
                    else
                    {
                        File.WriteAllText(matterMostConfigFile, matterMostUrl);
                    }

                    var matterMost = new MatterMost(matterMostUrl);

                    foreach (var temp in feedList)
                    {
                        Console.WriteLine($"拉取{temp}最新");
                        try
                        {
                            foreach (var blog in await GetBlog(temp))
                            {
                                if (publishBlogList.TryGetValue(blog.Url, out var time))
                                {
                                    if (DateTime.Now - time < minTime)
                                    {
                                        Console.WriteLine($"{blog.Title}最近{minTime.TotalDays:0}天发布过");
                                        continue;
                                    }
                                }

                                var distance = DateTime.Now - blog.Time;

                                if (distance > minTime)
                                {
                                    Console.WriteLine($"{blog.Title} 发布时间 {blog.Time} 距离当前{distance.TotalDays:0}");
                                    continue;
                                }

                                publishBlogList[blog.Url] = DateTime.Now;

                                matterMost.SendText($"[{blog.Title}]({blog.Url})");

                                Console.WriteLine($"发布 {blog.Title}");
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                    }

                    var removeList = new List<string>();
                    foreach (var (url, time) in publishBlogList)
                    {
                        if (DateTime.Now - time > TimeSpan.FromDays(7))
                        {
                            removeList.Add(url);
                        }
                    }

                    foreach (var temp in removeList)
                    {
                        publishBlogList.Remove(temp);
                    }

                    Console.WriteLine("发布完了，好累哇，休息一下");

                    Task.Delay(TimeSpan.FromMinutes(20)).Wait();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }


        private static async Task<List<Blog>> GetBlog(string url)
        {
            var blogList = new List<Blog>();
            var newsFeedService = new NewsFeedService(url);
            var syndicationItems = await newsFeedService.GetNewsFeed();
            foreach (var syndicationItem in syndicationItems)
            {
                var description =
                    syndicationItem.Description.Substring(0, Math.Min(200, syndicationItem.Description.Length));
                var time = syndicationItem.Published;
                var uri = syndicationItem.Links.FirstOrDefault()?.Uri;

                if (time < syndicationItem.LastUpdated)
                {
                    time = syndicationItem.LastUpdated;
                }

                blogList.Add(new Blog()
                {
                    Title = syndicationItem.Title,
                    Description = description,
                    Time = time.DateTime,
                    Url = uri?.AbsoluteUri
                });
            }

            return blogList;
        }
    }

    public class MatterMost
    {
        private readonly string _url;

        /// <inheritdoc />
        public MatterMost(string url)
        {
            _url = url;
        }

        public void SendText(string text)
        {
            var httpClient = new HttpClient();
            var json = JsonSerializer.Serialize(new
            {
                text = text
            });
            StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
            httpClient.PostAsync(_url, content);
        }
    }

    public class Blog
    {
        public string Title { get; set; }

        public string Url { get; set; }

        public string Description { get; set; }

        public DateTime Time { set; get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Title} {Time}\n{Url}";
        }
    }

    public class CnblogsFeed
    {
        /// <inheritdoc />
        public CnblogsFeed(string feedUri)
        {
            FeedUri = feedUri;
        }

        public async Task GetBlog()
        {
            var httpClient = new HttpClient();
            var str = await httpClient.GetStringAsync(FeedUri);
        }

        private string FeedUri { get; }
    }

    public class CsdnFeed
    {
        /// <inheritdoc />
        public CsdnFeed(string feedUri)
        {
            FeedUri = feedUri; //https://blog.csdn.net/sd7o95o/rss/list
        }

        public async Task GetBlog()
        {
            var httpClient = new HttpClient();
            var str = await httpClient.GetStringAsync(FeedUri);
            //<td class="title"><a href="https://blog.csdn.net/sD7O95O/article/details/102028746" target="_blank">[转]从0开始编写dapper核心功能、压榨性能、自己动手丰衣足食</a></td>
            var regex = new Regex(@"");
        }

        private string FeedUri { get; }
    }

    public class NewsFeedService
    {
        private readonly string _feedUri;

        public NewsFeedService(string feedUri)
        {
            _feedUri = feedUri;
        }

        private XmlFeedReader GetXmlFeedReader(string xml, XmlReader xmlReader)
        {
            var xDocument = XDocument.Load(new StringReader(xml));
            var rootName = xDocument.Root.Name;
            if (rootName.Namespace.NamespaceName.Contains("Atom", StringComparison.OrdinalIgnoreCase))
            {
                return new AtomFeedReader(xmlReader);
            }

            if (rootName.LocalName.Contains("feed", StringComparison.OrdinalIgnoreCase))
            {
                return new AtomFeedReader(xmlReader);
            }

            if (rootName.ToString().Contains("rss", StringComparison.OrdinalIgnoreCase))
            {
                return new RssFeedReader(xmlReader);
            }

            return new AtomFeedReader(xmlReader);
        }

        public async Task<List<ISyndicationItem>> GetNewsFeed()
        {
            var rssNewsItems = new List<ISyndicationItem>();

            var httpClient = new HttpClient()
            {
                Timeout = TimeSpan.FromMinutes(10)
            };
            var xml = await httpClient.GetStringAsync(_feedUri);

            using (var xmlReader = XmlReader.Create(new StringReader(xml)))
            {
                XmlFeedReader feedReader = GetXmlFeedReader(xml, xmlReader);
                while (await feedReader.Read())
                {
                    try
                    {
                        if (feedReader.ElementType == SyndicationElementType.Item)
                        {
                            ISyndicationItem item = await feedReader.ReadItem();
                            rssNewsItems.Add(item);
                        }
                    }
                    catch (Exception e)
                    {
                    }
                }
            }

            return rssNewsItems.OrderByDescending(p => p.LastUpdated).ToList();
        }
    }
}