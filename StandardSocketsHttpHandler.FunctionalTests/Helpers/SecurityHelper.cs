using System.Net.Security;

namespace System.Net.Http
{
    public class SecurityHelper
    {
        public static RemoteCertificateValidationCallback AllowAllCertificates { get; } = (RemoteCertificateValidationCallback)((sender, certificate, chain, SslPolicyErrors) => true);
    }
}
