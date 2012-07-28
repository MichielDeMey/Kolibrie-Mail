using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Crystalbyte.Equinox;
using Crystalbyte.Equinox.Imap;
using Kolibrie_Mail.Controller;
using Kolibrie_Mail.Model;
using Kolibrie_Mail.Protocols;
using Kolibrie_Mail.Views;

namespace Kolibrie_Mail
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Thread thread_downloadMessage = null;

        public MainWindow()
        {
            InitializeComponent();
            wbMailRenderer.ContextMenu = new ContextMenu();
            wbMailRenderer.ResetZoom();

            // Initial start of the program. Here we will check if the user already has an account or not.
            // v0.1 - simple check; will write a decent settings loader later
            //if (Properties.Settings.Default.Accounts == null || Properties.Settings.Default.Accounts.Count == 0)
            App.Log.Info("Checking application settings");
            if (Properties.Settings.Default.Accounts == null || String.IsNullOrWhiteSpace(Properties.Settings.Default.Accounts))
            {
                App.Log.Info("No settings found, showing the account creation window.");
                if (Properties.Settings.Default.Accounts == null)
                {
                    //Properties.Settings.Default.Accounts = new Accounts();
                    //Properties.Settings.Default.Save();
                }
                   

                // No accounts configured yet.
                // Show the Account creation window
                var acctCreation = new AccountCreation();
                acctCreation.ShowDialog();

                if (acctCreation.DialogResult.HasValue && acctCreation.DialogResult.Value)
                {
                    var acct = acctCreation.Account;
                    AccountController.Account = acct;

                    //Properties.Settings.Default.Accounts.Add(acct);
                    //Properties.Settings.Default.Save();
                }
                else
                {
                    Application.Current.Shutdown();
                    Environment.Exit(0);
                }
            }
            else // Load the existing accounts
            {
                App.Log.Info("Application settings found, now loading.");
                //AccountController.Account = Properties.Settings.Default.Accounts[0];
            }

            // Using Imap
            new Imap(AccountController.Account);
            App.Log.Info("Imap initialized.");

            // Now list the available folders
            var mailboxes = Imap.Client.List("", "*");
            App.Log.Info("Mailboxes loaded.");
            App.Log.Debug("Number of mailboxes: " + mailboxes.Mailboxes.Count());


            // Fill the list with mailboxes
            lstMailboxes.ItemsSource = mailboxes.Mailboxes;

            // Check for unread messages
            //CheckForUnreadMessages();

            // Always make sure there is a mailbox selected
            if(lstMailboxes.SelectedIndex < 0)
            {
                lstMailboxes.SelectedIndex = 1;
            }
        }

        private void AddMessagesOneByOne(Mailbox mailbox)
        {
            var indexRange = MailController.LastUid - 100;
            for (int i = indexRange; i < MailController.LastUid; i++)
            {
                // Open messages from database
                using (var session = App.DocumentStore.OpenSession())
                {
                    var message = session.Load<MessageContainer>(i.ToString());
                    if (message != null && message.Mailbox.Name.Equals(mailbox.Name))
                    {
                        AddEnvelopeSafely(message);
                    }
                }
            }
            //var databaseMessages = session.Query<MessageContainer>().Where(x => x.Uid > indexRange).OrderBy(x => x.Uid).ToList();
            //messageCollection.AddRange(databaseMessages);
            /*foreach (var messageContainer in databaseMessages)
            {
                AddEnvelopeSafely(messageContainer);
            }*/

            var messagesQuery = Imap.Client.Messages
            .Where(x => x.Uid > MailController.LastUid)
                //.Where(x => !x.Flags.HasFlag(MessageFlags.Seen))
            .Select(x => new MessageContainer
            {
                Uid = x.Uid,
                Envelope = x.Envelope,
                Size = x.Size
            }).ToList();

            // Save the emails to the database
            foreach (var messageContainer in messagesQuery)
            {
                if (messageContainer == null) continue;

                if (messageContainer.Uid > MailController.LastUid)
                {
                    MailController.LastUid = messageContainer.Uid;
                }
                /*else if (messageContainer.Uid == MailController.LastUid)
                {
                    continue;
                }*/

                messageContainer.Mailbox = mailbox;
                using (var session = App.DocumentStore.OpenSession())
                {
                    var loadedMessageContainer = session.Load<MessageContainer>(messageContainer.Uid.ToString());

                    // If there is no message already in the database. Store it.
                    if (loadedMessageContainer == null)
                    {
                        try
                        {
                            App.Log.Info("Writing mail " + messageContainer.Uid + " to the database.");
                            session.Store(messageContainer, messageContainer.Uid.ToString());
                            //messageCollection.Add(messageContainer);
                            AddEnvelopeSafely(messageContainer);
                        }
                        catch (Exception ex)
                        {
                            App.Log.Error(ex);
                            throw;
                        }


                        session.SaveChanges();
                    }
                    // We already have the message in the database. Load it.
                    else
                    {
                        //messageCollection.Add(loadedMessageContainer);
                        AddEnvelopeSafely(loadedMessageContainer);
                    }
                }
            }
        }

        private IEnumerable<MessageContainer> GetMessages(Mailbox mailbox)
        {
            var messageCollection = new List<MessageContainer>();
            //var selectedMailbox = Imap.Client.SelectedMailbox;

            /*var unreadMessagesQuery = Imap.Client.Messages
            .Where(x => !x.Flags.HasFlag(MessageFlags.Seen))
            .Select(x => new MessageContainer
            {
                Uid = x.Uid,
                Envelope = x.Envelope,
                BodyStructure = x.BodyStructure,
                Size = x.Size
            });*/

            using(var session = App.DocumentStore.OpenSession())
            {
                var indexRange = MailController.LastUid - 25;
                var databaseMessages = session.Query<MessageContainer>().Where(x => x.Uid > indexRange).OrderBy(x => x.Uid).ToList();
                messageCollection.AddRange(databaseMessages);
            }

            var messagesQuery = Imap.Client.Messages
            .Where(x => x.Uid > MailController.LastUid)
            //.Where(x => !x.Flags.HasFlag(MessageFlags.Seen))
            .Select(x => new MessageContainer
            {
                Uid = x.Uid,
                Envelope = x.Envelope,
                //BodyStructure = x.BodyStructure,
                Size = x.Size
            }).ToList();

            // Save the emails to the database
            foreach (var messageContainer in messagesQuery)
            {
                if (messageContainer == null) continue;

                if (messageContainer.Uid > MailController.LastUid)
                {
                    MailController.LastUid = messageContainer.Uid;
                }
                else if(messageContainer.Uid == MailController.LastUid)
                {
                    continue;
                }
                
                using (var session = App.DocumentStore.OpenSession())
                {
                    var loadedMessageContainer = session.Load<MessageContainer>(messageContainer.Uid.ToString());

                    // If there is no message already in the database. Store it.
                    if (loadedMessageContainer == null)
                    {
                        try
                        {
                            /*// Load the Envelope and the Size from the server
                            var container = messageContainer;

                            var message = Imap.Client.Messages
                                .Where(x => x.Uid == container.Uid)
                                .Select(x => new MessageContainer
                                {
                                    Uid = container.Uid,
                                    Mailbox = mailbox,
                                    Envelope = x.Envelope,
                                    Size = x.Size,
                                }).ToList().FirstOrDefault();*/

                            App.Log.Info("Writing mail " + messageContainer.Uid + " to the database.");
                            session.Store(messageContainer, messageContainer.Uid.ToString());
                            messageCollection.Add(messageContainer);
                        }
                        catch (Exception ex)
                        {
                            App.Log.Error(ex);
                            throw;
                        }
                    }
                        // We already have the message in the database. Load it.
                    else
                    {
                        messageCollection.Add(loadedMessageContainer);
                    }

                    session.SaveChanges();
                }
            }

            App.Log.Info("Messages saved to database.");
            
            //App.Log.Info("There are " + unreadMessagesQuery.AsEnumerable().Count() + " unread messages.");

            /*foreach (var unreadEnvelope in unreadMessagesQuery)
            {
                if (unreadEnvelope == null) continue;

                App.Log.Debug("Unread message: " + unreadEnvelope.Subject);
                messageCollection.Add(unreadEnvelope);
            }*/

            messageCollection.Reverse();
            //lstEnvelopes.ItemsSource = unreadCollection;
            return messageCollection;
        }

        private void lstMailboxes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedMailbox = (Mailbox) lstMailboxes.SelectedItem;
            lstEnvelopes.ItemsSource = null;
            lstEnvelopes.Items.Clear();

            try
            {
                App.Log.Info("Selecting mailbox: " + selectedMailbox.Name);
                Imap.Client.Select(selectedMailbox.Name);

                App.Log.Info("Getting messages from the server.");

                new Thread(() =>
                               {
                                //var messages = GetMessages(selectedMailbox);
                                //UpdateEnvelopesSafely(messages);
                                AddMessagesOneByOne(selectedMailbox);
                               }).Start();
            }
            catch (Exception ex)
            {
                App.Log.Error(ex);
                throw;
            }
        }

        private void AddEnvelopeSafely(MessageContainer message)
        {
            if(lstEnvelopes.Dispatcher.CheckAccess())
            {
                if(lstEnvelopes.Items.Count > 100)
                {
                    lstEnvelopes.Items.RemoveAt(lstEnvelopes.Items.Count-1);    
                }

                lstEnvelopes.Items.Insert(0, message);

                /*if (lstEnvelopes.Items.Count > 0)
                {
                    lstEnvelopes.SelectedIndex = 0;
                }*/
            }
            else
            {
                lstMailboxes.Dispatcher.BeginInvoke(new AddEnvelopeDelegate(AddEnvelopeSafely), message);
            }
        }

        private void UpdateEnvelopesSafely(IEnumerable<MessageContainer> messages)
        {
            if (lstEnvelopes.Dispatcher.CheckAccess())
            {
                lstEnvelopes.ItemsSource = messages;

                if (lstEnvelopes.Items.Count > 0)
                {
                    lstEnvelopes.SelectedIndex = 0;
                }
            }
            else
            {
                lstMailboxes.Dispatcher.BeginInvoke(new UpdateEnvelopesDelegate(UpdateEnvelopesSafely), messages);
            }
        }

        private delegate void AddEnvelopeDelegate(MessageContainer message);
        private delegate void UpdateEnvelopesDelegate(IEnumerable<MessageContainer> messages);

        private void lstEnvelopes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedMessage = (MessageContainer) lstEnvelopes.SelectedItem;
            var selectedMailbox = (Mailbox) lstMailboxes.SelectedItem;

            if (selectedMessage == null) return;

            App.Log.Info("Selected message: " + selectedMessage.Uid);

            // First check if there is already a thread running to do some work. 
            if (thread_downloadMessage != null && thread_downloadMessage.IsAlive)
            {
                // Abort the thread if it is already running
                thread_downloadMessage.Abort();
            }

            if (selectedMessage.BodyStructure == null)
            {
                var param = new Collection<Object> {selectedMailbox, selectedMessage};

                if (selectedMessage.HtmlView == null)
                {
                    thread_downloadMessage = new Thread(DownloadEmail);
                    // Finally start the thread with the parameters
                    thread_downloadMessage.Start(param);
                }
                else
                {
                    wbMailRenderer.LoadHTML(selectedMessage.HtmlView.Text);
                }
            }
            else
            {
                if (selectedMessage.HtmlView != null)
                {
                    wbMailRenderer.LoadHTML(selectedMessage.HtmlView.Text);
                }
                else // There is a bodystructure, but no view yet.
                {
                    var param = new Collection<Object>();
                    param.Add(selectedMailbox);
                    param.Add(selectedMessage);

                    if (selectedMessage.HtmlView == null)
                    {
                        thread_downloadMessage = new Thread(DownloadEmail);
                        // Finally start the thread with the parameters
                        thread_downloadMessage.Start(param);
                    }
                    else
                    {
                        wbMailRenderer.LoadHTML(selectedMessage.HtmlView.Text);
                    }
                    //App.Log.Error("Message " + selectedMessage.Uid + " has a BodyStructure, but no View!");
                }
            }
        }

        private void DownloadEmail(object param)
        {
            try
            {
                var oparam = (Collection<Object>) param;
                var selectedMailbox = (Mailbox)oparam[0];
                var selectedMessage = (MessageContainer)oparam[1];

                var client = AccountController.CreateClientByAccount(AccountController.Account);
                //var client = Imap.Client;
                client.Select(selectedMailbox.Name);

                client.DownloadProgressChanged += (sender, e) => UpdateProgressBarSafely(e.ReceivedBytes, e.TotalBytes);

                //var message = Imap.Client.FetchMessageByUid(selectedMessage.Uid);
                var bodyStructure = client.Messages.Where(x => x.Uid == selectedMessage.Uid).Select(x => x.BodyStructure).ToList().FirstOrDefault();
                if (bodyStructure != null)
                {
                    selectedMessage.BodyStructure = bodyStructure;

                    var viewinfo = bodyStructure.Views.Where(x => x.MediaType == "text/html").ToList().FirstOrDefault();
                    if (viewinfo != null)
                    {
                        View html;

                        if(!string.IsNullOrWhiteSpace(viewinfo.Token))
                        {
                            html = client.FetchView(viewinfo);
                        }
                        else
                        {
                            html = client.FetchView2(viewinfo);
                        }


                        selectedMessage.HtmlView = html;
                        //wbMailRenderer.NavigateToString(html.Text);
                        RenderHtmlEmailSafely(html.Text);

                        // Save to the database
                        using (var session = App.DocumentStore.OpenSession())
                        {
                            var msg = session.Load<MessageContainer>(selectedMessage.Uid.ToString());
                            msg.HtmlView = html;
                            session.SaveChanges();
                        }
                    }
                    else if (bodyStructure.Views.Where(x => x.MediaType == "text/plain").ToList().FirstOrDefault() != null)
                    {
                        var viewinfoPlain = bodyStructure.Views.Where(x => x.MediaType == "text/plain").ToList().FirstOrDefault(); 
                        if(viewinfoPlain != null)
                        {
                            var plain = client.FetchView2(viewinfoPlain);
                            selectedMessage.PlainView = plain;

                            RenderHtmlEmailSafely(plain.Text);

                            // Save to the database
                            using (var session = App.DocumentStore.OpenSession())
                            {
                                var msg = session.Load<MessageContainer>(selectedMessage.Uid.ToString());
                                msg.PlainView = plain;
                                session.SaveChanges();
                            }
                        }
                    }
                    else
                    {
                        App.Log.Debug("Message with Uid " + selectedMessage.Uid + " has no ViewInfo and therefor could not fetch the body of the message.");
                    }
                }
                else
                {
                    App.Log.Debug("Message with Uid " + selectedMessage.Uid + " has no BodyStructure.");
                }
            }
            catch (Exception ex)
            {
                App.Log.Error(ex.Message);
            } 
        }

        private void UpdateProgressBarSafely(int loaded, int total)
        {
            if (wbMailRenderer.Dispatcher.CheckAccess())
            {
                MessageProgressBar.Value = loaded;
                MessageProgressBar.Maximum = total;
            }
            else
            {
                wbMailRenderer.Dispatcher.BeginInvoke(new UpdateProgressBarDelegate(UpdateProgressBarSafely), loaded, total);
            }
        }

        private delegate void UpdateProgressBarDelegate(int loaded, int total);

        private void RenderHtmlEmailSafely(string html)
        {
            if(wbMailRenderer.Dispatcher.CheckAccess())
            {
                RenderHtmlEmail(html);
            }
            else
            {
                wbMailRenderer.Dispatcher.BeginInvoke(new RenderHtmlEmailDelegate(RenderHtmlEmailSafely), html); 
            }
        }
        
        private delegate void RenderHtmlEmailDelegate(string html);

        private void RenderHtmlEmail(string html)
        {
            //wbMailRenderer.NavigateToString(html);
            wbMailRenderer.LoadHTML(html);
        }

        private void wbMailRenderer_OpenExternalLink(object sender, Awesomium.Core.OpenExternalLinkEventArgs e)
        {
            var sInfo = new ProcessStartInfo(e.Url);
            Process.Start(sInfo);
        }

        private void wbMailRenderer_BeginNavigation(object sender, Awesomium.Core.BeginNavigationEventArgs e)
        {
            if(e.Url.Substring(0,7) == "http://")
            {
                var sInfo = new ProcessStartInfo(e.Url);
                Process.Start(sInfo);
                wbMailRenderer.Stop();
            }
        }

        /*private static void AuthenticateManually(ManualSaslAuthenticationRequiredEventArgs e, Account account)
        {
            new GmailAuthenticator().Authenticate(e.Client, e.UserCredentials, account);
        }*/
    }
}
