using Crystalbyte.Equinox.Imap;
using Kolibrie_Mail.Controller;
using Kolibrie_Mail.Model;

namespace Kolibrie_Mail.Protocols
{
    class Imap
    {
        public static ImapClient Client;

        public Imap(Account acct)
        {
            Client = AccountController.CreateClientByAccount(acct);

            App.Log.Info("User authentication finished (IMAP)");

            /*var obj = new ImapIdle();
            ImapController.IdleThread = new Thread(obj.ThreadStart);
            ImapController.IdleThread.Name = "IMAP IDLE Thread";
            ImapController.IdleThread.Start();*/
        }

    }
}
