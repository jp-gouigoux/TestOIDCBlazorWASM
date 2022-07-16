using DotCMIS;
using DotCMIS.Client;
using DotCMIS.Client.Impl;
using DotCMIS.Data.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecepteurMessages
{
    internal class ClientGED
    {
        public static string DeposerGED(byte[] contenu, string nomFichier)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters[DotCMIS.SessionParameter.BindingType] = BindingType.AtomPub;
            //parameters[DotCMIS.SessionParameter.BindingType] = BindingType.WebServices;
            //parameters[DotCMIS.SessionParameter.AtomPubUrl] = "http://localhost:9000/cmis/atom11";
            parameters[DotCMIS.SessionParameter.AtomPubUrl] = "http://localhost:9000/nuxeo/atom/cmis";
            //parameters[DotCMIS.SessionParameter.WebServicesRepositoryService] = "http://localhost:9000/cmis/services11/cmis?wsdl";
            //parameters[DotCMIS.SessionParameter.WebServicesAclService] = "http://localhost:9000/cmis/services11/cmis?wsdl";
            //parameters[DotCMIS.SessionParameter.WebServicesDiscoveryService] = "http://localhost:9000/cmis/services11/cmis?wsdl";
            //parameters[DotCMIS.SessionParameter.WebServicesMultifilingService] = "http://localhost:9000/cmis/services11/cmis?wsdl";
            //parameters[DotCMIS.SessionParameter.WebServicesNavigationService] = "http://localhost:9000/cmis/services11/cmis?wsdl";
            //parameters[DotCMIS.SessionParameter.WebServicesObjectService] = "http://localhost:9000/cmis/services11/cmis?wsdl";
            //parameters[DotCMIS.SessionParameter.WebServicesPolicyService] = "http://localhost:9000/cmis/services11/cmis?wsdl";
            //parameters[DotCMIS.SessionParameter.WebServicesRelationshipService] = "http://localhost:9000/cmis/services11/cmis?wsdl";
            //parameters[DotCMIS.SessionParameter.WebServicesRepositoryService] = "http://localhost:9000/cmis/services11/cmis?wsdl";
            //parameters[DotCMIS.SessionParameter.WebServicesVersioningService] = "http://localhost:9000/cmis/services11/cmis?wsdl";
            parameters[DotCMIS.SessionParameter.User] = "Administrator";
            parameters[DotCMIS.SessionParameter.Password] = "Administrator";
            SessionFactory factory = SessionFactory.NewInstance();
            ISession session = factory.GetRepositories(parameters)[0].CreateSession();

            IFolder root = session.GetRootFolder();

            ICmisObject result = session.GetObjectByPath("/repertoire");
            IFolder dossier;
            if (result == null)
            {
                IDictionary<string, object> folderProperties = new Dictionary<string, object>();
                folderProperties.Add(PropertyIds.Name, "repertoire");
                folderProperties.Add(PropertyIds.ObjectTypeId, "cmis:folder");
                dossier = root.CreateFolder(folderProperties);
            }
            else
            {
                dossier = (IFolder)result;
            }

            try
            {
                ICmisObject resFichier = session.GetObjectByPath("/repertoire/" + nomFichier);
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
