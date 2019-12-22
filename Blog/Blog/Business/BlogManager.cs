using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Blog.Data;
using Blog.Model;
using Microsoft.Extensions.Logging;

namespace Blog.Business
{
    public class BlogManager
    {
        public BlogManager(BlogContext blogContext, ILogger<BlogManager> logger)
        {
            BlogContext = blogContext;
            Logger = logger;
        }

        private string GetBlogFolder()
        {
            return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "blog");
        }

        public void UpdateBlog()
        {
            var folder = GetBlogFolder();

            BlogContext.BlogExcerptModel.RemoveRange(BlogContext.BlogExcerptModel);

            var blogExcerptModelList = new List<Blog.Model.BlogExcerptModel>();

            foreach (var file in Directory.GetFiles(folder, "*.md", SearchOption.AllDirectories))
            {
                try
                {
                    var str = File.ReadAllText(file);

                    var (title, excerpt, content) = ParseBlog(str);

                    var name = Path.GetFileNameWithoutExtension(file);
                    name = name.Replace("#", "");
                    name = name.Replace("+", "");
                    name = name.Replace(" ", "-");

                    blogExcerptModelList.Add(new BlogExcerptModel()
                    {
                        Title = title,
                        Excerpt = excerpt,
                        Url = $"/post/{name}.html",
                        FileName = file.Replace(folder + "\\", ""),
                        Time = File.GetLastWriteTime(file)
                    });
                }
                catch (Exception e)
                {
                    Logger.LogInformation(e.ToString());
                }
            }

            foreach (var blogExcerptModel in blogExcerptModelList.OrderByDescending(temp => temp.Time))
            {
                BlogContext.BlogExcerptModel.Add(blogExcerptModel);
            }

            BlogContext.SaveChanges();
        }


        private (string title, string excerpt, string content) ParseBlog(string str)
        {
            var stringReader = new StringReader(str);
            var title = stringReader.ReadLine() ?? "";

            if (title.StartsWith("#"))
            {
                title = title.Substring(1);
                title = title.Trim();
            }

            var excerpt = "";
            var line = stringReader.ReadLine();

            while (line != null)
            {
                if (line == "<!--more-->")
                {
                    break;
                }

                excerpt += line + "\r\n";

                line = stringReader.ReadLine();
            }

            var content = stringReader.ReadToEnd();

            return (title, excerpt, content);
        }

        public BlogContext BlogContext { get; }
        public ILogger<BlogManager> Logger { get; }

        public string GetBlog(string fileName)
        {
            return Path.Combine(GetBlogFolder(), fileName);
        }
    }
}