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

        [HttpGet]
        [AllowAnonymous]
        [Route("status")]
        public new ActionResult Statut()
        {
            return base.Statut();
        }

        [HttpGet]
        [Route("/api/[controller]")]
        public new IActionResult LecturePersonnes(
            [FromQuery(Name = "$skip")] int skip = 0,
            [FromQuery(Name = "$top")] int top = 10
        )
        {
            return base.LecturePersonnes(skip, top);
        }

        [HttpGet]
        [Route("/api/personnes/$count")]
        public new long LectureComptePersonnes()
        {
            return base.LectureComptePersonnes();
        }

        [HttpGet]
        [Route("/api/[controller]/{objectId}")]
        public new IActionResult LecturePersonne(string objectId)
        {
            return base.LecturePersonne(objectId);
        }

        // TODO : pour les écritures, ça sera intéressant de passer un nom d'user pour la traçabilité
        // et de voir comment on le value en mode certificat. Ce serait le nom du certificat éventuellement,
        // ce qui renforcerait le besoin d'en créer un pour chaque client d'API différent (bien
        // également pour ne pas avoir à révoquer tout le monde d'un coup en cas de fuite de certificat)
        [HttpPost]
        [Route("/api/[controller]")]
        public new IActionResult CreationPersonne([FromBody] DbPersonne personne)
        {
            return base.CreationPersonne(personne);
        }

        [HttpPatch]
        [Route("/api/[controller]/{objectId}")]
        public new IActionResult PatcherPersonne(string objectId, [FromBody] JsonPatchDocument patch)
        {
            return base.PatcherPersonne(objectId, patch);
        }

        [HttpDelete]
        [Route("/{objectId}")]
        public new void SuppressionPersonne(string objectId)
        {
            base.SuppressionPersonne(objectId);
        }

        [HttpDelete]
        [Route("/{objectId}")]
        public new void SuppressionToutesPersonnes()
        {
            base.SuppressionToutesPersonnes();
        }
    }
}
