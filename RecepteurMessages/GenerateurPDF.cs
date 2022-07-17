using Microsoft.Extensions.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TestOIDCBlazorWASM.Shared;

namespace RecepteurMessages
{
    internal class GenerateurPDF
    {
        private static Random hasard = new Random();
        private static HttpClient client = new HttpClient();

        public static byte[] GenererFiche(IConfiguration Configuration, Personne p)
        {
            // Récupération d'une image sur XKCD
            int maximumIndexImage = 614;
            int.TryParse(Configuration.GetSection("xkcd")["maximumIndexImage"], out maximumIndexImage);
            int indexImage = hasard.Next(maximumIndexImage) + 1;
            Task<string> definition = client.GetStringAsync(
                Configuration.GetSection("xkcd")["templateURLAPI"].Replace("{indexImage}", indexImage.ToString()));
            JsonDocument json = JsonDocument.Parse(definition.Result);
            string? urlPhoto = json.RootElement.GetProperty("img").GetString();
            Task<byte[]> image = client.GetByteArrayAsync(urlPhoto);

            // Génération d'un document complexe
            Document doc = Document.Create(document => {
                document.Page(page =>
                {
                    page.Margin(25, Unit.Millimetre);
                    page.Header().Text(p.Prenom + " " + p.Patronyme).FontSize(32).SemiBold().FontColor(Colors.Blue.Medium);
                    page.Content().Padding(20, Unit.Millimetre).Column(c =>
                    {
                        c.Item().Text("Détails sur la personne :");
                        c.Item().Text(Placeholders.LoremIpsum());
                        c.Item().Image(image.Result);
                    });
                });
            });

            // Retour du document généré, avec copie local pour debug éventuel
            return doc.GeneratePdf();
        }
    }
}
