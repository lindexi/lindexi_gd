using System.IO;
using System.Linq;
using System.Reflection;
using Blog.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Blog.Pages
{
    public class PostModel : PageModel
    {
        public MarkupString HtmlContent { get; private set; }

        public BlogModel Blog { set; get; }

        public IActionResult OnGet([FromRoute]string blogName)
        {
            var file = GetBlogFile(blogName);

            if (file is null)
            {
                return NotFound();
            }

            var str = System.IO.File.ReadAllText(file.FullName);

            str = ParseBlog(str);

            var html = Markdig.Markdown.ToHtml(str);

            HtmlContent = new MarkupString(html);

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

            ViewData["title"] = title;

            return stringReader.ReadToEnd();
        }

        private FileInfo GetBlogFile(string blogName)
        {
            var folder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "blog");

            blogName = blogName.Replace(".html", "");

            var fileList = Directory.GetFiles(folder);
            var file = fileList.FirstOrDefault(temp =>
              {
                  var name = Path.GetFileNameWithoutExtension(temp);
                  if (name == blogName)
                  {
                      return true;
                  }

                  name = name.Replace("#", "");
                  if (name == blogName)
                  {
                      return true;
                  }

                  name = name.Replace(" ", "-");
                  if (name == blogName)
                  {
                      return true;
                  }

                  return false;
              });

            return !string.IsNullOrEmpty(file) ? new FileInfo(file) : null;
        }
    }
}