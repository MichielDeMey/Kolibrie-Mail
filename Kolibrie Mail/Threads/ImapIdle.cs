using System.Threading;
using System.Windows;
using Crystalbyte.Equinox.Imap;
using Kolibrie_Mail.Controller;

namespace Kolibrie_Mail.Threads
{
    class ImapIdle
    {
        private static ImapClient _clientIdle;

        public void ThreadStart()
        {
            _clientIdle = AccountController.CreateClientByAccount(AccountController.Account);
            Thread.Sleep(1000);

            _clientIdle.StatusUpdateReceived += OnStatusUpdateReceived;
            _clientIdle.Select("INBOX");
            _clientIdle.StartIdle();
        }

        private static void OnStatusUpdateReceived(object sender, StatusUpdateReceivedEventArgs e)
        {
            var client = sender as ImapClient;
            if (client == null)
            {
                return;
            }

            // Respond to change notifications
            // i.E. client.Messages.Where(x.Uid > myLastKnownHighestUid).Select(x => x.Envelope) ...
            MessageBox.Show("New email");

            if (e.IsIdleUpdate)
            {
                e.IsIdleCancelled = false; // cancel IDLE session (false default)
            }
        }
    
    }
}
