using System.Security.Cryptography.X509Certificates;

namespace TestOIDCBlazorWASM.API
{
    public class CertificateValidation
    {
        public bool ValidateCertificate(X509Certificate2 clientCertificate)
        {
            string[] allowedThumbprints = { "ED3420BADD23CFBF3A3CE258C5F4502A2CF7C882" };
            return allowedThumbprints.Contains(clientCertificate.Thumbprint);
        }
    }
}
