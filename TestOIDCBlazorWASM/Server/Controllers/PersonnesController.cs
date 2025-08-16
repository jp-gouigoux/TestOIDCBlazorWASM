using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TestOIDCBlazorWASM.Shared;
using TestOIDCBlazorWASM.Work;

namespace TestOIDCBlazorWASM.Server.Controllers
{
    [Authorize] // Même si l'attribut Authorize est hérité, il faut le remettre pour que les spécialisations ci-dessous soient prises en compte
    [ApiController]
    public class PersonnesController : PersonnesControllerBase
    {
        public PersonnesController(IConfiguration config) : base(config)
        {
        }

        [Authorize(Policy = "lecteur")] // On ne surcharge que les autorisations qui le nécessitent, mais on ne peut pas passer en AllowAnonymous quelque chose qui hérite un Authorize
        [HttpGet] // Les expositions doivent être explicitement reprises car elles ne sont pas héritées et il y a alors conflit entre les GET et POST sur la même route
        [Route("/api/[controller]")] // Par contre, les routes sont obligatoirement réécrites
        public override IActionResult LecturePersonnes(
            [FromQuery(Name = "$skip")] int skip = 0,
            [FromQuery(Name = "$top")] int top = 10
        )
        {
            return base.LecturePersonnes(skip, top);
        }

        [Authorize(Policy = "administrateur")] // On ne surcharge que les autorisations qui le nécessitent, mais on ne peut pas passer en AllowAnonymous quelque chose qui hérite un Authorize
        [HttpPost] // Les expositions doivent être explicitement reprises car elles ne sont pas héritées et il y a alors conflit entre les GET et POST sur la même route
        [Route("/api/[controller]")] // Par contre, les routes sont obligatoirement réécrites
        public override async Task<IActionResult> CreationPersonne([FromBody] DbPersonne personne)
        {
            return await base.CreationPersonne(personne);
        }
    }
}
