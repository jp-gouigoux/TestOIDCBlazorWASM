using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TestOIDCBlazorWASM.Shared;

namespace TestOIDCBlazorWASM.Server.Controllers
{
    // TODO : Séparer cette API du serveur Blazor, en gérant ensuite le CORS
    [Authorize]
    [ApiController]
    public class PersonnesController : Controller
    {
        public PersonnesController(IConfiguration config)
        {
            string conn = config.GetValue<string>("PersonnesConnectionString");
            Database = new MongoClient(conn).GetDatabase("personnes");
        }

        private IMongoDatabase Database;

        private IMongoCollection<DbPersonne> Collection => Database.GetCollection<DbPersonne>("personnes");

        [HttpGet]
        [AllowAnonymous]
        [Route("status")]
        public ActionResult Statut()
        {
            if (Database.RunCommandAsync((Command<BsonDocument>)"{ping:1}").Wait(1000))
                return Ok();
            else
                return StatusCode(500);
        }

        [HttpGet]
        [Authorize(Policy = "lecteur")]
        [Route("/api/[controller]")]
        public IActionResult LecturePersonnes(
            [FromQuery(Name = "$skip")] int skip = 0,
            [FromQuery(Name = "$top")] int top = 10
        )
        {
            var query = Collection.Find(r => true);
            return new JsonResult(query.Skip(skip).Limit(top).ToList());
        }

        [HttpGet]
        [Route("/api/personnes/$count")] // TODO : voir pourquoi Blazor prend la main si on met un $count, mais OK avec un count
        public long LectureComptePersonnes()
        {
            return Collection.CountDocuments(r => true);
        }

        [HttpGet]
        [Authorize(Roles = "lecteur")]
        [Route("/api/[controller]/{objectId}")]
        public IActionResult LecturePersonne(string objectId)
        {
            var result = Collection.Find(item => item.ObjectId == objectId);
            if (result.CountDocuments() == 0)
                return new NotFoundResult();
            else
                return new JsonResult(result.First());
        }

        [HttpPost]
        [Authorize(Roles = "administrateur")]
        [Route("/api/[controller]")]
        public IActionResult CreationPersonne([FromBody] DbPersonne personne)
        {
            if (string.IsNullOrEmpty(personne.ObjectId))
                personne.ObjectId = Guid.NewGuid().ToString("N");
            Collection.InsertOneAsync(personne);

            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "personnes", durable: false, exclusive: false, autoDelete: false, arguments: null);
                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(personne));
                channel.BasicPublish(exchange: "", routingKey: "personnes", basicProperties: null, body: body);
            }

            Response.Headers.Add("Location", "/api/personnes/" + personne.ObjectId);
            return new StatusCodeResult(204);
        }

        [HttpPatch]
        [Authorize(Roles = "administrateur")]
        [Route("/api/[controller]/{objectId}")]
        public IActionResult PatcherPersonne(string objectId, [FromBody] JsonPatchDocument patch)
        {
            if (patch == null) return BadRequest();
            var personne = Collection.Find(item => item.ObjectId == objectId).First();
            patch.ApplyTo(personne);
            Collection.FindOneAndReplace(item => item.ObjectId == objectId, personne);
            return new ObjectResult(personne);
        }

        [HttpDelete]
        [Authorize(Roles = "administrateur")]
        [Route("/{objectId}")]
        public async void SuppressionPersonne(string objectId)
        {
            await Collection.DeleteManyAsync(Builders<DbPersonne>.Filter.Eq(item => item.ObjectId, objectId));
        }

        [HttpDelete]
        [Authorize(Roles = "administrateur")]
        [Route("/{objectId}")]
        public async void SuppressionToutesPersonnes()
        {
            await Collection.DeleteManyAsync(r => true);
        }
    }

    public class DbPersonne : Personne
    {
        [BsonId()]
        [BsonRepresentation(BsonType.ObjectId)]
        [JsonIgnore]
        public ObjectId TechnicalId { get; set; }
    }
}
