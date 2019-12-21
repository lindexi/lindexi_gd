using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Blog.Data;
using Blog.Model;

namespace Blog.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BlogController : ControllerBase
    {
        private readonly BlogContext _context;

        public BlogController(BlogContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Route("update")]
        public void UpdateBlog()
        {

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
