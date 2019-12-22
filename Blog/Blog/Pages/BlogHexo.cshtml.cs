using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Blog.Pages
{
    public class BlogHexoModel : PageModel
    {
        public void OnGet([FromRoute]int pageNumber)
        {
            PageNumber = pageNumber;
        }

        public int PageNumber { set; get; }
    }
}