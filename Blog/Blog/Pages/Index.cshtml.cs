using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Blog.Model;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Blog.Pages
{
    public class IndexModel : PageModel
    {
        public List<BlogExcerptModel> BlogExcerptModelList { set; get; }

        public void OnGet()
        {
            var folder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "blog");

            var n = 0;

            BlogExcerptModelList = new List<BlogExcerptModel>();

            foreach (var file in Directory.GetFiles(folder, "*.md"))
            {
                n++;

                var str = System.IO.File.ReadAllText(file);

                var (title, excerpt, content) = ParseBlog(str);

                var name = Path.GetFileNameWithoutExtension(file);
                name = name.Replace("#", "");
                name = name.Replace(" ", "-");

                BlogExcerptModelList.Add(new BlogExcerptModel()
                {
                    Title = title,
                    Excerpt = excerpt,
                    Url = $"/post/{name}.html"
                });

                if (n == 10)
                {
                    break;
                }
            }

        
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