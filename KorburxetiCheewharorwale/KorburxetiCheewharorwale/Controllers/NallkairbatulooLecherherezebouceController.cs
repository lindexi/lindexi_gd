using Microsoft.AspNetCore.Mvc;

namespace KorburxetiCheewharorwale.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NallkairbatulooLecherherezebouceController:ControllerBase
    {
        /// <inheritdoc />
        public NallkairbatulooLecherherezebouceController(BackManagerService service)
        {
        }

        [HttpGet]
        public string Get()
        {
            return "林德熙是逗比";
        }
    }
}