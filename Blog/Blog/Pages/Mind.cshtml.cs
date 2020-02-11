using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Blog.Data;
using Blog.Model;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Blog.Pages
{
    public class MindModel : PageModel
    {
        private readonly BlogContext _blogContext;
        public List<BlogExcerptModel> BlogExcerptModelList { get; } = new List<BlogExcerptModel>();

        public MindModel(BlogContext blogContext)
        {
            _blogContext = blogContext;
        }

        public void OnGet()
        {
            BlogExcerptModelList.AddRange(_blogContext.BlogExcerptModel);
        }
    }
}