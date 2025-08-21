using Microsoft.Extensions.Configuration;
using QuestPDF.Drawing;
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
            int.TryParse(Configuration["XKCD__MaximumIndexImage"], out maximumIndexImage);
            int indexImage = hasard.Next(maximumIndexImage) + 1;
            Console.WriteLine("Index image : " + indexImage);
            Console.WriteLine("TemplateURL : " + Configuration["XKCD__TemplateURLAPI"]);
            Task<string> definition = client.GetStringAsync(
                Configuration["XKCD__TemplateURLAPI"].Replace("{indexImage}", indexImage.ToString()));
            JsonDocument json = JsonDocument.Parse(definition.Result);
            string? urlPhoto = json.RootElement.GetProperty("img").GetString();
            Console.WriteLine("URL image : " + urlPhoto);
            Task<byte[]> image = client.GetByteArrayAsync(urlPhoto);

            // Chargement d'une fonte
            Console.WriteLine("Chargement des fontes");
            FontManager.RegisterFont(File.OpenRead("Lato-Regular.ttf"));
            FontManager.RegisterFont(File.OpenRead("Lato-Semibold.ttf"));

            // Génération d'un document complexe
            Console.WriteLine("Création du document");
            Document doc = Document.Create(document => {
                document.Page(page =>
                {
                    page.DefaultTextStyle(x => x.FontFamily("Lato"));
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

            // Retour du document généré
            Console.WriteLine("Génération du PDF");
            QuestPDF.Settings.License = LicenseType.Community; // If you use this code, check that you are compliant with QuestPDF license
            return doc.GeneratePdf();
        }
    }
}
