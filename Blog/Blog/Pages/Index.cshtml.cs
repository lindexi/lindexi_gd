using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Blog.Data;
using Blog.Model;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Blog.Pages
{
    public class IndexModel : PageModel
    {
        public IndexModel(BlogContext blogContext)
        {
            _blogContext = blogContext;
        }

        public List<BlogExcerptModel> BlogExcerptModelList { get; } = new List<BlogExcerptModel>();

        public void OnGet()
        {
            BlogExcerptModelList.AddRange(_blogContext.BlogExcerptModel.Take(10));
        }

        private readonly BlogContext _blogContext;
    }
}