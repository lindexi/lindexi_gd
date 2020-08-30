using System.Collections.Generic;
using System.Text;

namespace Tool.Shared.Model
{
    public class PageModel
    {
        public PageModel(string name)
        {
            Name = name;
        }

        public string Describe { set; get; }
        public string Name { get; }
    }
}
