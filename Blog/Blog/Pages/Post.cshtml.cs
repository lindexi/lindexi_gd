using System.IO;
using System.Linq;
using System.Reflection;
using Blog.Data;
using Blog.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace Blog.Pages
{
    public class PostModel : PageModel
    {
        private readonly BlogContext _blogContext;
        private readonly IConfiguration _configuration;
        public MarkupString HtmlContent { get; private set; }

        public PostModel(BlogContext blogContext, IConfiguration configuration)
        {
            _blogContext = blogContext;
            _configuration = configuration;
        }

        public BlogModel Blog { set; get; }

        public IActionResult OnGet([FromRoute] string blogName)
        {
            var folder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "blog");

            var url = $"/post/{blogName}";

            var blogExcerptModel = _blogContext.BlogExcerptModel.FirstOrDefault(temp => temp.Url == url);


            if (blogExcerptModel is null)
            {
                ViewData["title"] = _configuration["Title"];
                // lang=html
                HtmlContent = new MarkupString(@"<h3>找不到这篇博客</h3>");
            }
            else
            {
                ViewData["title"] = blogExcerptModel.Title;
                ViewData["BlogTitle"] = blogExcerptModel.Title;

                var file = Path.Combine(folder, blogExcerptModel.FileName);

                var str = System.IO.File.ReadAllText(file);

                str = ParseBlog(str);

                var html = Markdig.Markdown.ToHtml(str);

                HtmlContent = new MarkupString(html);
            }


            return Page();
        }

        private string ParseBlog(string str)
        {
            var stringReader = new StringReader(str);
            var title = stringReader.ReadLine() ?? "";

            if (title.StartsWith("#"))
            {
                title = title.Substring(1);
                title = title.Trim();
            }

            Blog = new BlogModel()
            {
                Title = title
            };

            return stringReader.ReadToEnd();
        }
    }
}