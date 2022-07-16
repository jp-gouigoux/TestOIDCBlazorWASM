using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TestOIDCBlazorWASM.Shared;

namespace TestOIDCBlazorWASM.Work
{
    /// <summary>
    /// On ne spécifie pas d'autorisation par défaut, car c'est le choix des implémentations d'exposition, et la surcharge ne fonctionne pas bien
    /// </summary>
    [Authorize]
    public abstract class PersonnesControllerBase : Controller
    {
        public PersonnesControllerBase(IConfiguration config)
        {
            string conn = config.GetValue<string>("PersonnesConnectionString");
            Database = new MongoClient(conn).GetDatabase("personnes");
        }

        private IMongoDatabase Database;

        private IMongoCollection<DbPersonne> Collection => Database.GetCollection<DbPersonne>("personnes");

        [AllowAnonymous]
        [HttpGet]
        [Route("/api/[controller]/status")]
        public virtual ActionResult Statut()
        {
            if (Database.RunCommandAsync((Command<BsonDocument>)"{ping:1}").Wait(1000))
                return Ok();
            else
                return StatusCode(500);
        }

        [HttpGet]
        [Route("/api/[controller]")]
        public virtual IActionResult LecturePersonnes(
            [FromQuery(Name = "$skip")] int skip = 0,
            [FromQuery(Name = "$top")] int top = 10
        )
        {
            var query = Collection.Find(r => true);
            return new JsonResult(query.Skip(skip).Limit(top).ToList());
        }

        [HttpGet]
        [Route("/api/[controller]/$count")]
        public virtual long LectureComptePersonnes()
        {
            return Collection.CountDocuments(r => true);
        }

        [HttpGet]
        [Route("/api/[controller]/{objectId}")]
        public virtual IActionResult LecturePersonne(string objectId)
        {
            var result = Collection.Find(item => item.ObjectId == objectId);
            if (result.CountDocuments() == 0)
                return new NotFoundResult();
            else
                return new JsonResult(result.First());
        }

        [HttpPost]
        [Route("/api/[controller]")]
        public virtual IActionResult CreationPersonne([FromBody] DbPersonne personne)
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
        [Route("/api/[controller]/{objectId}")]
        public virtual IActionResult PatcherPersonne(string objectId, [FromBody] JsonPatchDocument patch)
        {
            if (patch == null) return BadRequest();
            var personne = Collection.Find(item => item.ObjectId == objectId).First();
            patch.ApplyTo(personne);
            Collection.FindOneAndReplace(item => item.ObjectId == objectId, personne);
            return new ObjectResult(personne);
        }

        [HttpDelete]
        [Route("/api/[controller]/{objectId}")]
        public virtual async void SuppressionPersonne(string objectId)
        {
            await Collection.DeleteManyAsync(Builders<DbPersonne>.Filter.Eq(item => item.ObjectId, objectId));
        }

        [HttpDelete]
        [Route("/api/[controller]/all")]
        public virtual async void SuppressionToutesPersonnes()
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
