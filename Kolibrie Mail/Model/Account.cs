using System.Net;
using Crystalbyte.Equinox.Security;

namespace Kolibrie_Mail.Model
{
    public class Account
    {
        public NetworkCredential Credential { get; set; }
        public string Host { get; set; }
        public SecurityPolicies Security { get; set; }
        public string Name { get; set; }
        public string XOAuthKey { get; set; }
    }
}
