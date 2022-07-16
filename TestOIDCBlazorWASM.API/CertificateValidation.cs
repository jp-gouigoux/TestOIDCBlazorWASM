using System.Security.Cryptography.X509Certificates;

namespace TestOIDCBlazorWASM.API
{
    public class CertificateValidation
    {
        public bool ValidateCertificate(X509Certificate2 clientCertificate)
        {
            // TODO : voir comment passer en secrets au moins le mot de passe
            //var cert = new X509Certificate2(@"C:\Users\jpgou\OneDrive\Securite\ClientCertificate\dotnet\dev_cert.pfx", "Secret123");
            //return clientCertificate.Thumbprint == cert.Thumbprint;

            string[] allowedThumbprints = { "6BDA0F3604953B518079FEE3E1DB18A3E1CCE9DE" };
            return allowedThumbprints.Contains(clientCertificate.Thumbprint);
        }
    }
}
