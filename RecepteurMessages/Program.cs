using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RecepteurMessages;
using System.Text;
using System.Text.Json;
using TestOIDCBlazorWASM.Shared;

//byte[] contenu = RecepteurMessages.GenerateurPDF.GenererFiche(new Personne() { Prenom = "JP", Patronyme = "Gouigoux" });
//RecepteurMessages.ClientGED.DeposerGED(contenu, "Fichier-JP-Gouigoux.pdf");
//return;

var factory = new ConnectionFactory() { HostName = "localhost" };
using (var connection = factory.CreateConnection())
using (var channel = connection.CreateModel())
{
    channel.QueueDeclare(queue: "personnes", durable: false, exclusive: false, autoDelete: false, arguments: null);
    var consumer = new EventingBasicConsumer(channel);
    consumer.Received += (model, ea) =>
    {
        var body = ea.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);

        Personne? p = JsonSerializer.Deserialize<Personne>(message);
        if (p is not null)
        {
            Console.WriteLine(" [x] Traitement de la personne {0} {1}", p.Prenom, p.Patronyme);
            
            // On commence par générer une fiche PDF spécifique à la personne
            byte[] pdf = GenerateurPDF.GenererFiche(p);

            // Cette fiche est ensuite déposée dans une GED (n'importe laquelle, du moment qu'elle supporte CMIS)
            string nomFichier = "Fichier-" + p.Prenom + "-" + p.Patronyme + ".pdf";
            string idDoc = RecepteurMessages.ClientGED.DeposerGED(pdf, nomFichier);

            // On attend un peu, de façon à simuler une opération bien complexe
            Thread.Sleep(5000);

            // Appel de l'API Personnes pour patcher le code du document associé
            // (même si le mieux sera de faire une requête CMIS dynamique à chaque appel)
            RecepteurMessages.ClientAPIPersonnes.AjouterFicheSurPersonne(
                p, 
                new Uri("http://localhost:9000/nuxeo/atom/cmis/default/content/" + nomFichier + "?id=" + idDoc));

            // Si tout s'est bien passé, on peut prévenir que le message est bien traité
            channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
        }
    };
    channel.BasicConsume(queue: "personnes", autoAck: false, consumer: consumer);

    Console.WriteLine(" Press [enter] to exit.");
    Console.ReadLine();
}