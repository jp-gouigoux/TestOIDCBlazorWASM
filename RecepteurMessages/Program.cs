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
  .AddUserSecrets<Program>()
  .AddCommandLine(args)
  .Build();

string? MotDePasseCertificatClient = Configuration.GetSection("Securite")["MotDePasseCertificatClient"];
string? FichierMotDePasse = Configuration.GetSection("Securite")["FichierMotDePasseCertificatClient"];
if (!string.IsNullOrEmpty(FichierMotDePasse))
    MotDePasseCertificatClient = File.ReadAllText(FichierMotDePasse);
if (string.IsNullOrEmpty(MotDePasseCertificatClient))
    throw new ArgumentException("Un paramétrage du mot de passe pour le certificat client, par fichier ou par valeur, est obligatoire dans la section Securite");

string ServeurRabbitMQ = Configuration["RabbitMQ__HoteServeur"] ?? "localhost";
string UserNameRabbitMQ = Configuration["RabbitMQ__Utilisateur"] ?? "guest";
string PasswordRabbitMQ = Configuration["RabbitMQ__MotDePasse"] ?? "guest";
if (ServeurRabbitMQ == null) throw new ArgumentException("L'argument de ligne de commande RabbitMQ__HoteServeur doit obligatoirement spécifier un serveur RabbitMQ");
var factory = new ConnectionFactory() { HostName = ServeurRabbitMQ, UserName = UserNameRabbitMQ, Password = PasswordRabbitMQ };
using (var connection = await factory.CreateConnectionAsync())
using (var channel = await connection.CreateChannelAsync())
{
    await channel.QueueDeclareAsync(queue: Configuration["RabbitMQ__NomQueueMessagesCreationPersonnes"] ?? "personnes", durable: false, exclusive: false, autoDelete: false, arguments: null);
    var consumer = new AsyncEventingBasicConsumer(channel);
    consumer.ReceivedAsync += async (model, ea) => 
    {
        try
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            Personne? p = JsonSerializer.Deserialize<Personne>(message);
            if (p is not null)
            {
                Console.WriteLine("Tentative de traitement de la personne {0} {1}", p.Prenom, p.Patronyme);

                // On commence par générer une fiche PDF spécifique à la personne
                byte[] pdf = GenerateurPDF.GenererFiche(Configuration, p);

                // Cette fiche est ensuite déposée dans une GED (n'importe laquelle, du moment qu'elle supporte CMIS)
                string templateNomFichier = Configuration.GetSection("GED")["ModeleNomFichierPourFichesPersonnes"] ?? "Fichier-{prenom}-{patronyme}.pdf";
                string nomFichier = templateNomFichier.Replace("{prenom}", p.Prenom).Replace("{patronyme}", p.Patronyme);
                string idDoc = ClientGED.DeposerGED(Configuration, pdf, nomFichier);

                // On attend un peu, de façon à simuler une opération bien complexe
                // et laisser le temps au navigateur client de voir la liste sans la fiche
                int delaiSupplementaireTraitement = 0;
                if (int.TryParse(Configuration["DelaiSupplementaireTraitementEnMillisecondes"], out delaiSupplementaireTraitement))
                    Thread.Sleep(delaiSupplementaireTraitement);

                // Appel de l'API Personnes pour patcher le code du document associé
                // (même si le mieux sera de faire une requête CMIS dynamique à chaque appel)
                string? modeleURLExpositionDirecteDocuments = Configuration["GED__ModeleURLExpositionDirecteDocuments"];
                if (string.IsNullOrEmpty(modeleURLExpositionDirecteDocuments))
                    throw new ArgumentException("L'attribut ModeleURLExpositionDirecteDocuments doit être spécifié dans la section de configuration GED");
                ClientAPIPersonnes.AjouterFicheSurPersonne(
                    Configuration,
                    p,
                    new Uri(modeleURLExpositionDirecteDocuments
                        .Replace("{nomFichier}", nomFichier)
                        .Replace("{idDoc}", idDoc)));

                // Si tout s'est bien passé, on peut prévenir que le message est bien traité
                Console.WriteLine("==> traitement réussi");
            }
        }
        catch (Exception excep)
        {
#if DEBUG
            Console.WriteLine(excep);
#endif
            Console.WriteLine("==> erreur de traitement de type {0}", excep.GetType());
        }
        finally
        {
            // Pour simplifier dans un premier temps, on acknowledge toujours, que le traitement se soit bien passé ou pas
            await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
        }
    };
    await channel.BasicConsumeAsync(queue: Configuration["RabbitMQ__NomQueueMessagesCreationPersonnes"], autoAck: false, consumer: consumer);

    Console.WriteLine("Appuyer la touche ENTREE pour terminer le programme");
    Console.ReadLine();
}