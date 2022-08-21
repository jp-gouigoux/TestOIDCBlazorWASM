using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using TestOIDCBlazorWASM.Shared;

namespace RecepteurMessages
{
    internal class ClientAPIPersonnes
    {
        private static HttpClient client = null;

        public async static void AjouterFicheSurPersonne(IConfiguration Configuration, Personne p, Uri fiche)
        {
            // Quand c'est un service qui appelle une API, c'est plus logique de s'authentifier avec un certificat
            // et le premier code exemple utilisé est celui indiqué sur https://stackoverflow.com/questions/40014047/add-client-certificate-to-net-core-httpclient
            if (client == null)
            {
                var handler = new HttpClientHandler();
                handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                handler.SslProtocols = SslProtocols.Tls12;
                string MotDePasseCertificatClient = Configuration.GetSection("Securite")["MotDePasseCertificatClient"];
                string FichierMotDePasse = Configuration["Securite__FichierMotDePasseCertificatClient"];
                if (!string.IsNullOrEmpty(FichierMotDePasse))
                    MotDePasseCertificatClient = File.ReadAllText(FichierMotDePasse);
                var cert = new X509Certificate2(
                    Configuration.GetSection("Securite")["CheminFichierCertificatClient"],
                    MotDePasseCertificatClient);
                handler.ClientCertificates.Add(cert);
                handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                client = new HttpClient(handler);
            }

            using (var requete = new HttpRequestMessage(HttpMethod.Patch, Configuration["URLBaseServiceAPI"] + "/api/personnes/" + p.ObjectId))
            {
                requete.Content = new StringContent(
                    "[{ \"path\": \"/urlFiche\", \"op\": \"replace\", \"value\": \"" + fiche + "\" }]", 
                    Encoding.UTF8, 
                    "application/json-patch+json");
                using (HttpResponseMessage reponse = await client.SendAsync(requete))
                {
                    // Pour l'instant, si la personne n'existe plus, on avale juste l'exception, puisque le but est d'ajouter un document dessus
                    // Idéalement, on devrait faire le test en amont pour ne pas travailler pour rien, mais le cas est censé rester rare
                    if (reponse.StatusCode == HttpStatusCode.NotFound)
                        return;
                    reponse.EnsureSuccessStatusCode();
                }
            }
        }
    }
}
