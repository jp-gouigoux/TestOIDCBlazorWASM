using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecepteurMessages;

internal class ClientGED
{
    public static async Task<string> DeposerGEDAsync(IConfiguration Configuration, byte[] contenu, string nomFichier)
    {
        if (Configuration["GED:URLBrowser"] == null)
            throw new ArgumentException("Le paramètre de configuration GED URLBrowser doit être renseigné pour déposer un document dans la GED");
        if (Configuration["GED:ServiceAccountName"] == null)
            throw new ArgumentException("Le paramètre de configuration GED ServiceAccountName doit être renseigné pour déposer un document dans la GED");
        if (Configuration["GED:ServiceAccountPassword"] == null)
            throw new ArgumentException("Le paramètre de configuration GED ServiceAccountPassword doit être renseigné pour déposer un document dans la GED");

        var client = new PureHttpCMISClient(
            Configuration["GED:URLBrowser"]!,
            "default",
            Configuration["GED:ServiceAccountName"]!,
            Configuration["GED:ServiceAccountPassword"]!);

        string dossierClassement = Configuration["GED:NomRepertoireStockageFichesPersonnes"] ??
            throw new ArgumentException("Le paramètre de configuration GED NomRepertoireStockageFichesPersonnes doit être renseigné pour déposer un document dans la GED");
        if (await client.ExistsFolderAsync(dossierClassement) == false)
            await client.CreateFolderAsync(dossierClassement);

        string fichierTemp = Path.GetTempFileName();
        await File.WriteAllBytesAsync(fichierTemp, contenu);
        try
        {
            string docId = await client.CreateDocumentAsync(
                dossierClassement,
                nomFichier,
                fichierTemp
            );
            Console.WriteLine("Doc créé : " + docId);
            return docId;
        }
        finally
        {
            File.Delete(fichierTemp);
        }
    }
}
