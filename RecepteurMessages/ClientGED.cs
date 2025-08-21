using DotCMIS;
using DotCMIS.Client;
using DotCMIS.Client.Impl;
using DotCMIS.Data.Impl;
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
        if (Configuration["GED__URLBrowser"] == null)
            throw new ArgumentException("Le paramètre de configuration GED URLBrowser doit être renseigné pour déposer un document dans la GED");
        if (Configuration["GED__ServiceAccountName"] == null)
            throw new ArgumentException("Le paramètre de configuration GED ServiceAccountName doit être renseigné pour déposer un document dans la GED");
        if (Configuration["GED__ServiceAccountPassword"] == null)
            throw new ArgumentException("Le paramètre de configuration GED ServiceAccountPassword doit être renseigné pour déposer un document dans la GED");

        var client = new PureHttpCMISClient(
            Configuration["GED__URLBrowser"]!,
            "default",
            Configuration["GED__ServiceAccountName"]!,
            Configuration["GED__ServiceAccountPassword"]!);

        string dossierClassement = Configuration["GED__NomRepertoireStockageFichesPersonnes"] ??
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
