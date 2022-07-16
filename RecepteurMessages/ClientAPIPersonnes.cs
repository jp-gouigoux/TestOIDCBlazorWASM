using System;
using System.Collections.Generic;
using System.Linq;
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

        public async static void AjouterFicheSurPersonne(Personne p, Uri fiche)
        {
            // Quand c'est un service qui appelle une API, c'est plus logique de s'authentifier avec un certificat
            // et le premier code exemple utilisé est celui indiqué sur https://stackoverflow.com/questions/40014047/add-client-certificate-to-net-core-httpclient
            if (client == null)
            {
                var handler = new HttpClientHandler();
                handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                handler.SslProtocols = SslProtocols.Tls12;
                handler.ClientCertificates.Add(new X509Certificate2("cert.crt")); // TODO : continuer ici
                client = new HttpClient(handler);
            }

            using (var requete = new HttpRequestMessage(HttpMethod.Patch, "https://localhost:7070/api/personnes/" + p.ObjectId))
            {
                requete.Content = new StringContent(
                    "[{ \"path\": \"/urlFiche\", \"op\": \"replace\", \"value\": \"" + fiche + "\" }]", 
                    Encoding.UTF8, 
                    "application/json-patch+json");
                using (HttpResponseMessage reponse = await client.SendAsync(requete))
                {
                    reponse.EnsureSuccessStatusCode();
                }
            }
        }
    }
}
