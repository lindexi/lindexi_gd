using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace NecalemberexjkynGijemneada.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MattermostController : ControllerBase
    {
        [HttpGet("Foo")]
        public void Foo()
        {
            var httpClient = new HttpClient();
            StringContent content = new StringContent("{\"text\": \"林德熙是逗比\"}",Encoding.UTF8, "application/json");
            httpClient.PostAsync("http://127.0.0.1:8065/hooks/xjkyn7ks1pn7xeho1f5ifxqhxh", content);
        }
    }
}