using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using TestOIDCBlazorWASM.Work;

namespace TestOIDCBlazorWASM.API.Controllers
{
    [Authorize]
    [ApiController]
    public class PersonnesController : PersonnesControllerBase
    {
        public PersonnesController(IConfiguration config) : base(config)
        {
        }
    }
}
