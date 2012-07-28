#region Microsoft Public License (Ms-PL)

// // Microsoft Public License (Ms-PL)
// // 
// // This license governs use of the accompanying software. If you use the software, you accept this license. If you do not accept the license, do not use the software.
// // 
// // 1. Definitions
// // 
// // The terms "reproduce," "reproduction," "derivative works," and "distribution" have the same meaning here as under U.S. copyright law.
// // 
// // A "contribution" is the original software, or any additions or changes to the software.
// // 
// // A "contributor" is any person that distributes its contribution under this license.
// // 
// // "Licensed patents" are a contributor's patent claims that read directly on its contribution.
// // 
// // 2. Grant of Rights
// // 
// // (A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
// // 
// // (B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.
// // 
// // 3. Conditions and Limitations
// // 
// // (A) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.
// // 
// // (B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, your patent license from such contributor to the software ends automatically.
// // 
// // (C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution notices that are present in the software.
// // 
// // (D) If you distribute any portion of the software in source code form, you may do so only under this license by including a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object code form, you may only do so under a license that complies with this license.
// // 
// // (E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular purpose and non-infringement.

#endregion

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Crystalbyte.Equinox.Imap;
using Crystalbyte.Equinox.Security;
using Crystalbyte.Equinox.Security.Authentication;

namespace Crystalbyte.Equinox.DemoApp
{
    public partial class ApplicationForm : Form
    {
        private readonly ObservableCollection<Account> _accounts = new ObservableCollection<Account>();
        private readonly Dictionary<string, TreeNode> _activityPendingNodes = new Dictionary<string, TreeNode>();

        public ApplicationForm()
        {
            InitializeComponent();
            _accounts.CollectionChanged += (sender, e) => OnAccountCollectionChanged(e);
        }

        private MyMessage ActiveMessage
        {
            get { return MessageGrid.SelectedRows.Count > 0 ? MessageGrid.SelectedRows[0].DataBoundItem as MyMessage : null; }
        }

        private object OnAccountCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action) {
                case NotifyCollectionChangedAction.Add:
                    AddNodeToTreeView((Account) e.NewItems[0]);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    break;
                default:
                    break;
            }

            return null;
        }

        private void AddNodeToTreeView(Account account)
        {
            var node = new TreeNode(account.Name) {Tag = new AccountStateObject {Account = account}};

            MailboxTreeView.Nodes.Add(node);
        }

        private void CreateAccountButtonClick(object sender, EventArgs e)
        {
            using (var form = new AccountCreationForm()) {
                var result = form.ShowDialog();
                if (result != DialogResult.Cancel) {
                    _accounts.Add(form.Account);
                }
            }
        }

        private void OnMailboxTreeViewNodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            var node = e.Node;
            if (node == null) {
                return;
            }

            if (node.Tag is AccountStateObject) {
                var stateObject = (AccountStateObject) node.Tag;
                if (node.Nodes.Count > 0) {
                    // nothing to do here yet, mailboxes have been loaded already
                } else {
                    StoreNodeForFastLookup(stateObject, node, "");
                    FetchNextLevelOfMailboxesAsync("", stateObject);
                }
            }

            if (node.Tag is Mailbox) {
                var mailbox = (Mailbox) node.Tag;

                if (node.Nodes.Count == 0) {
                    StoreNodeForFastLookup(mailbox.AccountState, node, mailbox.Fullname);
                    FetchNextLevelOfMailboxesAsync(mailbox.Fullname, mailbox.AccountState);
                }

                messageBindingSource.DataSource = mailbox.Messages;
                if (mailbox.Messages.Count == 0) {
                    FetchEnvelopesAsync(mailbox);
                }
            }
        }

        private void StoreNodeForFastLookup(AccountStateObject stateObject, TreeNode node, string mailboxName)
        {
            var key = stateObject.Account.Name + "/" + mailboxName;
            if (!_activityPendingNodes.ContainsKey(key)) {
                _activityPendingNodes.Add(key, node);
            }
        }

        private ImapClient CreateClientByAccount(Account account)
        {
            try {
                var client = new ImapClient {Security = account.Security};
                //client.ManualSaslAuthenticationRequired += (sender, e) => AuthenticateManually(e, account);
                var port = client.Security == SecurityPolicies.Explicit ? ImapPorts.Ssl : ImapPorts.Default;
                client.Connect(account.Host, port);
                client.Authenticate(account.Credential, SaslMechanics.Login);
                return client;
            }
            catch (Exception ex) {
                LogSafely(ex.Message);
                throw;
            }
        }

        private static void AuthenticateManually(ManualSaslAuthenticationRequiredEventArgs e, Account account)
        {
            //new GmailAuthenticator().Authenticate(e.Client, e.UserCredentials, account);
        }

        private void FetchEnvelopesAsync(Mailbox mailbox)
        {
            new Thread(() =>
                           {
                               try {
                                   using (var client = CreateClientByAccount(mailbox.AccountState.Account)) {
                                       client.Select(mailbox.Fullname);

                                       var query = client.Messages.Where(x => x.Date < DateTime.Today.AddDays(1)).Select(x => new MyMessage {Envelope = x.Envelope, Uid = x.Uid});

                                       foreach (var message in query) {
                                           message.Mailbox = mailbox;
                                           AddMessageToViewSafely(mailbox, message);
                                       }
                                   }
                               }
                               catch (Exception ex) {
                                   LogSafely(ex.Message);
                               }
                           }).Start();
        }

        private void AddMessageToViewSafely(Mailbox mailbox, MyMessage message)
        {
            if (InvokeRequired) {
                this.Invoke(() => AddMessageToView(mailbox, message));
            } else {
                AddMessageToView(mailbox, message);
            }
        }

        private static void AddMessageToView(Mailbox mailbox, MyMessage message)
        {
            mailbox.Messages.Add(message);
        }

        private void FetchNextLevelOfMailboxesAsync(string root, AccountStateObject stateObject)
        {
            new Thread(() =>
                           {
                               try {
                                   using (var client = CreateClientByAccount(stateObject.Account)) {
                                       // get next level


                                       var wildcards = (root + "/%").TrimStart('/');
                                       var response = client.LSub("", wildcards);
                                       var converted = response.Mailboxes.Select(x => new Mailbox {Fullname = x.Name, Name = ExtractName(x.Name, x.Delimiter), AccountState = stateObject});

                                       var key = stateObject.Account.Name + "/" + root;
                                       AddMailboxToTreeSafely(converted, key);
                                       stateObject.IsBusy = false;
                                   }
                               }
                               catch (Exception ex) {
                                   LogSafely(ex.Message);
                               }
                           }).Start();
        }

        private void AddMailboxToTreeSafely(IEnumerable<Mailbox> mailboxes, string key)
        {
            if (InvokeRequired) {
                this.Invoke(() => AddMailboxToTree(mailboxes, key));
            } else {
                AddMailboxToTree(mailboxes, key);
            }
        }

        private void AddMailboxToTree(IEnumerable<Mailbox> mailboxes, string key)
        {
            foreach (var mailbox in mailboxes) {
                var child = CreateNodeFromMailbox(mailbox);
                var node = _activityPendingNodes[key];
                node.Nodes.Add(child);
            }

            if (!_activityPendingNodes.ContainsKey(key)) {
                return;
            }

            _activityPendingNodes[key].Expand();
            _activityPendingNodes.Remove(key);
        }

        private static TreeNode CreateNodeFromMailbox(Mailbox mailbox)
        {
            var node = new TreeNode(mailbox.Name) {Tag = mailbox};
            return node;
        }

        private static string ExtractName(string fullname, char delimiter)
        {
            return fullname.Split(new[] {delimiter}).Last();
        }

        private void LogSafely(string message)
        {
            if (InvokeRequired) {
                this.Invoke(() => { OutputTextBox.Text += message + Environment.NewLine; });
            } else {
                OutputTextBox.Text += message + Environment.NewLine;
            }
        }

        private void UpdateProgressBarSafely(MyMessage message, int loaded, int total)
        {
            if (InvokeRequired) {
                this.BeginInvoke(() =>
                                     {
                                         if (ActiveMessage != message) {
                                             return;
                                         }
                                         MessageProgressBar.Value = loaded;
                                         MessageProgressBar.Maximum = total;
                                     });
            } else {
                if (ActiveMessage != message) {
                    return;
                }
                MessageProgressBar.Value = loaded;
                MessageProgressBar.Maximum = total;
            }
        }

        private void FetchMessageAsync(MyMessage message)
        {
            new Thread(() =>
                           {
                               try {
                                   using (var client = CreateClientByAccount(message.Mailbox.AccountState.Account)) {
                                       client.Select(message.Mailbox.Fullname);

                                       client.DownloadProgressChanged += (sender, e) => UpdateProgressBarSafely(message, e.ReceivedBytes, e.TotalBytes);

                                       message.Message = client.FetchMessageByUid(message.Uid);

                                       if (ActiveMessage == null) {
                                           return;
                                       }

                                       // only display message when user has not moved on
                                       if (ActiveMessage == message) {
                                           DisplayMessageSafely(message);
                                       }
                                   }
                               }
                               catch (Exception ex) {
                                   LogSafely(ex.Message);
                               }
                           }).Start();
        }

        private void DisplayMessageSafely(MyMessage message)
        {
            if (InvokeRequired) {
                this.Invoke(() => DisplayMessage(message));
            }
        }

        private void OnMessageGridSelectionChanged(object sender, EventArgs e)
        {
            if (MessageGrid.SelectedRows.Count < 1) {
                return;
            }

            var message = MessageGrid.SelectedRows[0].DataBoundItem as MyMessage;
            if (message == null) {
                return;
            }

            FetchMessageAsync(message);
        }

        private void DisplayMessage(MyMessage message)
        {
            AttachmentListView.Items.Clear();
            MessageViewer.Navigate("about:blank");

            foreach (var attachment in message.Message.Attachments) {
                var item = new ListViewItem(attachment.Filename) {Tag = attachment, BackColor = Color.Orange, ForeColor = Color.WhiteSmoke};
                AttachmentListView.Items.Add(item);
            }

            if (message.Message.Views.Count == 0) {
                return;
            }

            if (message.Message.HasHtmlView) {
                var html = message.Message.GetHtmlView();
                DisplayView(html.Text);
            } else {
                var plain = message.Message.GetPlainTextView();

                if (plain == null) {
                    if (message.Message.Views.Count > 0) {
                        plain = message.Message.Views.First();
                    }
                }

                if (plain == null) {
                    LogSafely("No view found ... shouldnt be ...");
                    return;
                }

                // we want line breaks in text documents
                if (string.IsNullOrEmpty(plain.Text)) {
                    return;
                }

                DisplayView(plain.Text.Replace(Environment.NewLine, "<br />"));
            }
        }

        private void DisplayView(string text)
        {
            MessageViewer.DocumentText = text;
        }

        private void OnListViewItemDoubleClicked(object sender, EventArgs e)
        {
            if (AttachmentListView.SelectedItems.Count == 1) {
                try {
                    var attachment = AttachmentListView.SelectedItems[0].Tag as Attachment;
                    OpenFile(attachment);
                }
                catch (Exception ex) {
                    LogSafely(ex.Message);
                }
            }
        }

        private static void OpenFile(Attachment attachment)
        {
            var extension = string.Empty;
            if (attachment.Filename.Any(x => x == '.')) {
                extension = attachment.Filename.Split(new[] {'.'}).Last();
            }

            var filename = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()) + "." + extension;
            File.WriteAllBytes(filename, attachment.Bytes);
            Process.Start(filename);
        }
    }
}