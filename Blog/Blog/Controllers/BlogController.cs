using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Blog.Business;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Blog.Data;
using Blog.Model;
using Microsoft.Extensions.Configuration;

namespace Blog.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BlogController : ControllerBase
    {
        private readonly BlogManager _blogManager;
        private readonly BlogContext _context;
        private readonly IConfiguration _configuration;

        public BlogController(BlogManager blogManager, BlogContext context, IConfiguration configuration)
        {
            _blogManager = blogManager;
            _context = context;
            _configuration = configuration;
        }

        [Route("/feed.xml")]
        public ContentResult GetFeed()
        {
            DateTime today = DateTime.Now;
            string rfc822 = today.ToString("r");

            var blogSite = _configuration["BlogSite"];
            if (blogSite.EndsWith("/"))
            {
                blogSite = blogSite.Substring(0, blogSite.Length - 1);
            }

            var itemString = new StringBuilder();

            foreach (var temp in _context.BlogExcerptModel.Take(10))
            {
                itemString.Append($@"<item>
                    <title>{temp.Title}</title>
                    <description>{temp.Excerpt}</description>
                    <pubDate>{ temp.Time.ToString("r")}</pubDate>
                    <link>{blogSite}/{temp.Url}</link>
                    <guid isPermaLink=""true"">{blogSite}{temp.Url}</guid>
                  </item>");

                itemString.Append("\r\n");
            }
            /*
           {{% for tag in post.tags %}}
                    <category>{{{{ tag | xml_escape }}}}</category>
                    {{% endfor %}}
                    {{% for cat in post.categories %}}
                    <category>{{{{ cat | xml_escape }}}}</category>
                    {{% endfor %}}
             */

            // lang=xml
            var xmlString = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<rss version=""2.0"" xmlns:atom=""http://www.w3.org/2005/Atom"">
  <channel>
    <title>林德熙的博客</title>
    <description>欢迎小伙伴访问我的博客</description>
    <link>{blogSite}</link>
    <atom:link href=""{blogSite}/feed.xml"" rel=""self"" type=""application/rss+xml""/>
    <pubDate>{rfc822}</pubDate>
    <lastBuildDate>{rfc822}</lastBuildDate>
    <generator>Blazor</generator>
    {itemString}
  </channel>
</rss>
";

            //application/xml
            return new ContentResult
            {
                ContentType = "application/xml",
                Content = xmlString,
                StatusCode = 200
            };
        }


        [HttpGet]
        [Route("update")]
        public void UpdateBlog()
        {
            _blogManager.UpdateBlog();
        }

        // GET: api/Blog
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BlogExcerptModel>>> GetBlogExcerptModel()
        {
            return await _context.BlogExcerptModel.ToListAsync();
        }

        // GET: api/Blog/5
        [HttpGet("{id}")]
        public async Task<ActionResult<BlogExcerptModel>> GetBlogExcerptModel(int id)
        {
            var blogExcerptModel = await _context.BlogExcerptModel.FindAsync(id);

            if (blogExcerptModel == null)
            {
                return NotFound();
            }

            return blogExcerptModel;
        }

        // PUT: api/Blog/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPut("{id}")]
        public async Task<IActionResult> PutBlogExcerptModel(int id, BlogExcerptModel blogExcerptModel)
        {
            if (id != blogExcerptModel.Id)
            {
                return BadRequest();
            }

            _context.Entry(blogExcerptModel).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BlogExcerptModelExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Blog
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPost]
        public async Task<ActionResult<BlogExcerptModel>> PostBlogExcerptModel(BlogExcerptModel blogExcerptModel)
        {
            _context.BlogExcerptModel.Add(blogExcerptModel);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetBlogExcerptModel", new { id = blogExcerptModel.Id }, blogExcerptModel);
        }

        // DELETE: api/Blog/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<BlogExcerptModel>> DeleteBlogExcerptModel(int id)
        {
            var blogExcerptModel = await _context.BlogExcerptModel.FindAsync(id);
            if (blogExcerptModel == null)
            {
                return NotFound();
            }

            _context.BlogExcerptModel.Remove(blogExcerptModel);
            await _context.SaveChangesAsync();

            return blogExcerptModel;
        }

        private bool BlogExcerptModelExists(int id)
        {
            return _context.BlogExcerptModel.Any(e => e.Id == id);
        }
    }
}
