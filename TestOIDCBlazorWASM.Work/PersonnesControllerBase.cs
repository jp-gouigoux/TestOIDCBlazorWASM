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
        private IMongoDatabase Database;
        private string NomCollectionPersonnes { get; init; }
        private IMongoCollection<DbPersonne> Collection;
        private string NomServeurMOM { get; init; }
        private string NomQueueMessages { get; init; }
        private string NomUtilisateurMOM { get; init; }
        private string MotDePasseMOM { get; init; }
        private string ModeleEnteteHTTPLocation { get; init; }

        public PersonnesControllerBase(IConfiguration config)
        {
            // Paramétrage base NoSQL
            string conn = config["PersistanceNoSQL:PersonnesConnectionString"];
            string NomBaseDeDonneesPersonnes = config["PersistanceNoSQL:PersonnesDatabaseName"];
            Database = new MongoClient(conn).GetDatabase(NomBaseDeDonneesPersonnes);
            NomCollectionPersonnes = config["PersistanceNoSQL:PersonnesCollectionName"];
            Collection = Database.GetCollection<DbPersonne>("personnes");

            // Paramétrage MOM
            NomServeurMOM = config["RabbitMQ:HoteServeur"] ?? "localhost";
            NomQueueMessages = config["RabbitMQ:NomQueueMessagesCreationPersonnes"] ?? "personnes";
            NomUtilisateurMOM = config["RabbitMQ:Utilisateur"] ?? "guest";
            MotDePasseMOM = config["RabbitMQ:MotDePasse"] ?? "guest";

            // Paramétrage API
            ModeleEnteteHTTPLocation = config["ModeleEnteteHTTPLocation"];
        }

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

        // TODO : pour les écritures, ça sera intéressant de passer un nom d'user pour la traçabilité
        // et de voir comment on le value en mode certificat. Ce serait le nom du certificat éventuellement,
        // ce qui renforcerait le besoin d'en créer un pour chaque client d'API différent (bien
        // également pour ne pas avoir à révoquer tout le monde d'un coup en cas de fuite de certificat)
        [HttpPost]
        [Route("/api/[controller]")]
        public virtual async Task<IActionResult> CreationPersonne([FromBody] DbPersonne personne)
        {
            if (string.IsNullOrEmpty(personne.ObjectId))
                personne.ObjectId = Guid.NewGuid().ToString("N");
            await Collection.InsertOneAsync(personne);

            var factory = new ConnectionFactory() { HostName = this.NomServeurMOM, UserName = this.NomUtilisateurMOM, Password = this.MotDePasseMOM };
            using (var connection = await factory.CreateConnectionAsync())
            using (var channel = await connection.CreateChannelAsync())
            {
                await channel.QueueDeclareAsync(queue: this.NomQueueMessages, durable: false, exclusive: false, autoDelete: false, arguments: null);
                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(personne));
                var props = new BasicProperties();
                await channel.BasicPublishAsync(exchange: "", routingKey: this.NomQueueMessages, mandatory:true, basicProperties: props, body: body);
            }

            Response.Headers.Add("Location", ModeleEnteteHTTPLocation.Replace("${object_id}", personne.ObjectId));
            return new StatusCodeResult(204);
        }

        [HttpPatch]
        [Route("/api/[controller]/{objectId}")]
        public virtual IActionResult PatcherPersonne(string objectId, [FromBody] JsonPatchDocument patch)
        {
            if (patch == null) return BadRequest();
            var personne = Collection.Find(item => item.ObjectId == objectId).FirstOrDefault();
            if (personne == null) return NotFound();
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
