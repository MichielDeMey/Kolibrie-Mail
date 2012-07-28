using System;
using System.Threading;
using Crystalbyte.Equinox.Imap;
using Crystalbyte.Equinox.Security;
using Crystalbyte.Equinox.Security.Authentication;
using Kolibrie_Mail.Model;

namespace Kolibrie_Mail.Controller
{
    static class AccountController
    {
        public static Account Account { get; set; }

        public static ImapClient CreateClientByAccount(Account account)
        {
            try
            {
                var client = new ImapClient { Security = account.Security };
                //client.ManualSaslAuthenticationRequired += (sender, e) => AuthenticateManually(e, account);
                var port = client.Security == SecurityPolicies.Explicit ? ImapPorts.Ssl : ImapPorts.Default;
                client.Connect(account.Host, port);
                //Thread.Sleep(100);
                client.Authenticate(account.Credential, SaslMechanics.Login);
                //Thread.Sleep(500);

                return client;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }
    }
}
