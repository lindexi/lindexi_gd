using Blog.Model;
using System.Collections.Generic;

namespace BlogTool
{
    class BlogPageModel
    {
        public int PageCount { set; get; }

        public List<BlogExcerptModel> BlogList { set; get; }
    }
}
