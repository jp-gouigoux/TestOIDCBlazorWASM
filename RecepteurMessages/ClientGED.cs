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

namespace RecepteurMessages
{
    internal class ClientGED
    {
        public static string DeposerGED(IConfiguration Configuration, byte[] contenu, string nomFichier)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters[DotCMIS.SessionParameter.BindingType] = BindingType.AtomPub;
            parameters[DotCMIS.SessionParameter.AtomPubUrl] = Configuration.GetSection("ged")["urlAtomPub"];
            parameters[DotCMIS.SessionParameter.User] = Configuration.GetSection("ged")["serviceAccountName"];
            parameters[DotCMIS.SessionParameter.Password] = Environment.GetEnvironmentVariable("GED_SERVICE_ACCOUNT_PWD") ?? "";
            SessionFactory factory = SessionFactory.NewInstance();
            ISession session = factory.GetRepositories(parameters)[0].CreateSession();

            IFolder root = session.GetRootFolder();

            ICmisObject result = null;
            try { session.GetObjectByPath("/" + Configuration.GetSection("ged")["nomRepertoireStockageFichesPersonnes"]); }
            catch { }
            IFolder dossier;
            if (result == null)
            {
                IDictionary<string, object> folderProperties = new Dictionary<string, object>();
                folderProperties.Add(PropertyIds.Name, Configuration.GetSection("ged")["nomRepertoireStockageFichesPersonnes"]);
                folderProperties.Add(PropertyIds.ObjectTypeId, "cmis:folder");
                dossier = root.CreateFolder(folderProperties);
            }
            else
            {
                dossier = (IFolder)result;
            }

            try
            {
                ICmisObject resFichier = session.GetObjectByPath("/" + Configuration.GetSection("ged")["nomRepertoireStockageFichesPersonnes"] + "/" + nomFichier);
                IDocument docExistant = (IDocument)resFichier;
                return docExistant.Id;
            }
            catch { }

            IDictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add(PropertyIds.Name, nomFichier);
            properties.Add(PropertyIds.ObjectTypeId, "cmis:document");
            ContentStream contentStream = new ContentStream
            {
                FileName = nomFichier,
                MimeType = "application/pdf",
                Length = contenu.Length,
                Stream = new MemoryStream(contenu)
            };
            IDocument docCreated = dossier.CreateDocument(properties, contentStream, null);
            return docCreated.Id;
        }
    }
}
