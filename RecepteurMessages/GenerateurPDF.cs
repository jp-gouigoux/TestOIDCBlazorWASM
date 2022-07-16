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

        public static byte[] GenererFiche(Personne p)
        {
            // Récupération d'une image sur XKCD
            int codeImage = hasard.Next(614) + 1;
            Task<string> definition = client.GetStringAsync("https://xkcd.com/" + codeImage.ToString() + "/info.0.json");
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
#if DEBUG
            string FichePersonne = @"D:\Temp\fichepersonne.pdf";
            File.Delete(FichePersonne);
            doc.GeneratePdf(FichePersonne);
#endif
            return doc.GeneratePdf();
        }
    }
}
