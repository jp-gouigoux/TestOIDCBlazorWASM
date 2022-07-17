using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RecepteurMessages;
using System.Text;
using System.Text.Json;
using TestOIDCBlazorWASM.Shared;

// Démarrage du mécanisme de configuration, qui n'est pas injecté en mode console
// Ne pas oublier de passer le fichier appsettings.json en fichier de type contenu pour le projet et d'activer la copie
// Les variables d'environnement ou la ligne de commande seront utilisées pour passer les paramètres confidentiels,
// comme le mot de passe du certificat client à utiliser pour s'authentifier à l'API paramétrée en ClientCertificate
IConfiguration Configuration = new ConfigurationBuilder()
  .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
  .AddEnvironmentVariables()
  .AddCommandLine(args)
  .Build();

var factory = new ConnectionFactory() { HostName = Configuration.GetSection("rabbitMQ")["hoteServeur"] };
using (var connection = factory.CreateConnection())
using (var channel = connection.CreateModel())
{
    channel.QueueDeclare(queue: Configuration.GetSection("rabbitMQ")["nomQueueMessagesCreationPersonnes"], durable: false, exclusive: false, autoDelete: false, arguments: null);
    var consumer = new EventingBasicConsumer(channel);
    consumer.Received += (model, ea) =>
    {
        var body = ea.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);

        Personne? p = JsonSerializer.Deserialize<Personne>(message);
        if (p is not null)
        {
            Console.WriteLine("Traitement de la personne {0} {1}", p.Prenom, p.Patronyme);
            
            // On commence par générer une fiche PDF spécifique à la personne
            byte[] pdf = GenerateurPDF.GenererFiche(Configuration, p);

            // Cette fiche est ensuite déposée dans une GED (n'importe laquelle, du moment qu'elle supporte CMIS)
            string nomFichier = Configuration.GetSection("ged")["modeleNomFichierPourFichesPersonnes"]
                .Replace("{prenom}", p.Prenom)
                .Replace("{patronyme}", p.Patronyme);
            string idDoc = ClientGED.DeposerGED(Configuration, pdf, nomFichier);

            // On attend un peu, de façon à simuler une opération bien complexe
            // et laisser le temps au navigateur client de voir la liste sans la fiche
            int delaiSupplementaireTraitement = 0;
            if (int.TryParse(Configuration["delaiSupplementaireTraitementEnMillisecondes"], out delaiSupplementaireTraitement))
                Thread.Sleep(delaiSupplementaireTraitement);

            // Appel de l'API Personnes pour patcher le code du document associé
            // (même si le mieux sera de faire une requête CMIS dynamique à chaque appel)
            ClientAPIPersonnes.AjouterFicheSurPersonne(
                Configuration,
                p, 
                new Uri(Configuration.GetSection("ged")["modeleURLExpositionDirecteDocuments"]
                    .Replace("{nomFichier}", nomFichier)
                    .Replace("{idDoc}", idDoc)));

            // Si tout s'est bien passé, on peut prévenir que le message est bien traité
            channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
        }
    };
    channel.BasicConsume(queue: Configuration.GetSection("rabbitMQ")["nomQueueMessagesCreationPersonnes"], autoAck: false, consumer: consumer);

    Console.WriteLine("Appuyer la touche ENTREE pour terminer le programme");
    Console.ReadLine();
}