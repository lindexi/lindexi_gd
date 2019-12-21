using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Blog.Model;

namespace Blog.Data
{
    public class BlogContext : DbContext
    {
        public BlogContext(DbContextOptions<BlogContext> options)
            : base(options)
        {
        }


        public DbSet<Blog.Model.BlogExcerptModel> BlogExcerptModel { get; set; }

        public void Init()
        {
            var folder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "blog");

            BlogExcerptModel.RemoveRange(BlogExcerptModel);

            var blogExcerptModelList = new List<Blog.Model.BlogExcerptModel>();

            foreach (var file in Directory.GetFiles(folder, "*.md", SearchOption.AllDirectories))
            {
                var str = System.IO.File.ReadAllText(file);

                var (title, excerpt, content) = ParseBlog(str);

                var name = Path.GetFileNameWithoutExtension(file);
                name = name.Replace("#", "");
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

            foreach (var blogExcerptModel in blogExcerptModelList.OrderByDescending(temp => temp.Time))
            {
                BlogExcerptModel.Add(blogExcerptModel);
            }

            SaveChanges();
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
    }
}
