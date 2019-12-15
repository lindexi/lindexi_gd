using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Blog.Model
{
    public class BlogModel
    {
        public string Title { get; set; }
    }

    public class BlogExcerptModel : BlogModel
    {
        public string Url { get; set; }

        public string Excerpt { set; get; }
    }
}