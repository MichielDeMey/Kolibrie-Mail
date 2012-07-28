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
using System.Diagnostics;
using System.Linq;
using System.Net;
using Crystalbyte.Equinox.Imap.Commands;
using Crystalbyte.Equinox.Imap.Linq;
using Crystalbyte.Equinox.Imap.Responses;
using Crystalbyte.Equinox.Imap.Text;
using Crystalbyte.Equinox.Mime;
using Crystalbyte.Equinox.Mime.Text;
using Crystalbyte.Equinox.Security;
using Crystalbyte.Equinox.Security.Authentication;

namespace Crystalbyte.Equinox.Imap
{
    public sealed class ImapClient : SecureClient
    {
        private readonly DownloadProgressChangedEventArgs _downloadProgressChangedEventArgs;

        public ImapClient()
        {
            Messages = new Query<IMessageQueryable>(new ImapMessageQueryProvider(this));
            ResponseTimeout = TimeSpan.FromSeconds(20);
            UpdateDownloadProgressTriggerChunkSize = Size.FromBytes(500);
            UsePeek = true;
            _downloadProgressChangedEventArgs = new DownloadProgressChangedEventArgs(0, 1);
        }

        /// <summary>
        ///   This property sets the recurring chunk size after which a download progress update event is being thrown.
        ///   As default every 500 bytes the update is being triggered.
        /// </summary>
        internal Size UpdateDownloadProgressTriggerChunkSize { get; set; }

        /// <summary>
        ///   Gets the ascertained server capablities listes in a dictionary as key-value pairs.
        /// </summary>
        public ImapServerCapability ServerCapability { get; private set; }

        /// <summary>
        ///   Gets a queryable list of messages. This property is the entrypoint to the LINQ provider.
        /// </summary>
        public IQueryable<IMessageQueryable> Messages { get; private set; }

        /// <summary>
        ///   Gets whether the client was authenticated to the server.
        /// </summary>
        public bool IsAuthenticated { get; internal set; }

        /// <summary>
        ///   Gets or sets whether a fetch operation for a message part will trigger the message to be marked as read on the server.
        ///   If set to true the message will remain unread even if fetched. The default value is true.
        /// </summary>
        public bool UsePeek { get; set; }

        /// <summary>
        ///   Gets or sets the timeout after which any receiving operation will be terminated by the client.
        ///   The client will then trigger a TimeoutException. 
        ///   When setting the client to IDLE the timeout will be automatically increased to 20 minutes.
        /// </summary>
        public TimeSpan ResponseTimeout { get; set; }

        /// <summary>
        ///   Gets or sets the currently selected mailbox. This property is modified by the Select, Examine and Close methods.
        /// </summary>
        public string SelectedMailbox { get; private set; }

        /// <summary>
        ///   Gets whether the client is currently in an idling state.
        /// </summary>
        public bool IsIdling { get; private set; }

        /// <summary>
        ///   This event is fired on any multiline response and reflects the progress of the current download.
        /// </summary>
        public event EventHandler<DownloadProgressChangedEventArgs> DownloadProgressChanged;

        internal void InvokeDownloadProgressChanged(int received, int total)
        {
            var handler = DownloadProgressChanged;
            if (handler != null) {
                _downloadProgressChangedEventArgs.TotalBytes = total;
                _downloadProgressChangedEventArgs.ReceivedBytes = received;
                handler(this, _downloadProgressChangedEventArgs);
            }
        }

        /// <summary>
        ///   This event is being thrown if no mutual supported authentication mechanism could be negotiated by the server and the client.
        ///   The user will have the opportunity to manually authenticate himself to the server.
        /// </summary>
        public event EventHandler<ManualSaslAuthenticationRequiredEventArgs> ManualSaslAuthenticationRequired;

        /// <summary>
        ///   Attempt to notify the application that automatic sasl authentication is not possible.
        ///   The ManualSaslAuthenticationRequired event will be thrown.
        /// </summary>
        /// <param name = "credential">The credentials provided by the user.</param>
        /// <param name = "client">The current instance of the client, responsible for the connection.</param>
        internal bool InvokeManualSaslAuthenticationRequired(NetworkCredential credential, ImapClient client)
        {
            var handler = ManualSaslAuthenticationRequired;
            if (handler != null) {
                var e = new ManualSaslAuthenticationRequiredEventArgs(credential, client);
                handler(this, e);
                return e.IsAuthenticated;
            }
            return false;
        }

        /// <summary>
        ///   This event is being thrown after the server sends a status update to the client.
        ///   It is important to know that ANY command can trigger a status update, therefor this feature
        ///   is implemented as an event.
        /// </summary>
        public event EventHandler<StatusUpdateReceivedEventArgs> StatusUpdateReceived;

        internal void InvokeStatusUpdateReceived(string line)
        {
            var handler = StatusUpdateReceived;
            if (handler != null) {
                var e = new StatusUpdateReceivedEventArgs(SelectedMailbox);
                e.MailboxInfo.InjectLine(line);
                handler(this, e);
            }
        }

        internal void InvokeStatusUpdateReceived(StatusUpdateReceivedEventArgs e)
        {
            var handler = StatusUpdateReceived;
            if (handler != null) {
                handler(this, e);
            }
        }

        protected override void FetchCapabilities()
        {
            var command = new ImapCommand(CommandStrings.Capability);
            var reader = SendAndReceive(command);
            ServerCapability = reader.ReadCapabilities();
        }

        protected override bool IsTlsSupported()
        {
            return ServerCapability.IsTlsSupported;
        }

        protected override bool IssueStartTlsCommand(string host)
        {
            var command = new ImapCommand(CommandStrings.StartTls);
            Send(command);
            return Receive().IsOk;
        }

        /// <summary>
        ///   Sends a command to the server and returns a reader capable of receiving line by line.
        /// </summary>
        /// <param name = "command">The command to issue.</param>
        /// <param name = "processStatusUpdatesAutomatically">This param defines whether received status updates will be processed automatically.</param>
        /// <returns>Returns a response reader.</returns>
        public ImapResponseReader SendAndReceive(ImapCommand command, bool processStatusUpdatesAutomatically = true)
        {
            Send(command);
            return Receive(processStatusUpdatesAutomatically);
        }

        /// <summary>
        ///   Receives a response from the server.
        /// </summary>
        /// <returns>Returns a response reader.</returns>
        public ImapResponseReader Receive(bool processStatusUpdatesAutomatically = true)
        {
            var reader = new ImapResponseReader(this);
            reader.ReadNextLine();
            if (processStatusUpdatesAutomatically) {
                CheckForStatusUpdates(reader);
            }
            return reader;
        }

        /// <summary>
        ///   Sends a command to the server.
        /// </summary>
        /// <param name = "command">The command to issue.</param>
        public void Send(ImapCommand command)
        {
            var commandString = command.ToString();
            WriteLine(commandString);
        }

        /// <summary>
        ///   Sends a command to the server.
        /// </summary>
        /// <param name = "command">The command to issue.</param>
        public void SendAsync(ImapCommand command)
        {
            var commandString = command.ToString();
            WriteLineAsync(commandString);
        }

        internal string ReceiveSingleLine()
        {
            return ReadLine();
        }

        internal void CheckForStatusUpdates(ImapResponseReader reader)
        {
            StatusUpdateReceivedEventArgs e = null;
            while (true) {
                if (!reader.IsStatusUpdate) {
                    break;
                }

                if (e == null) {
                    e = new StatusUpdateReceivedEventArgs(SelectedMailbox);
                }

                e.MailboxInfo.InjectLine(reader.CurrentLine);
                reader.ReadNextLine();
                continue;
            }

            if (e != null) {
                InvokeStatusUpdateReceived(e);
            }
        }

        /// <summary>
        ///   Authenticates the client to the server using the XOAUTH mechanism.
        /// </summary>
        /// <param name = "key">The XOAUTH authetication key.</param>
        /// <returns>Returns true on success, false otherwise.</returns>
        public bool AuthenticateXOAuth(string key)
        {
            var authenticator = new ImapAuthenticator(this);
            IsAuthenticated = authenticator.AuthenticateXOAuth(key);
            return IsAuthenticated;
        }

        /// <summary>
        ///   Authenticates the client to the server using the best supported SASL mechanism.
        /// </summary>
        /// <param name = "username">The username, obviously.</param>
        /// <param name = "password">The users password.</param>
        /// <param name = "mechanic">The SASL mechanism to use. This param is optional and can be left blank (0x0000), if done so, the best fitting mechanism will be chosen automatically.</param>
        /// <returns>Returns true on success, false otherwise.</returns>
        public bool Authenticate(string username, string password, SaslMechanics mechanic = (SaslMechanics) 0x0000)
        {
            var credentials = new NetworkCredential(username, password);
            IsAuthenticated = Authenticate(credentials, mechanic);
            return IsAuthenticated;
        }

        /// <summary>
        ///   Authenticates the client to the server using the best supported SASL mechanism.
        /// </summary>
        /// <param name = "credentials">The user credentials.</param>
        /// <param name = "mechanic">The SASL mechanism to use. This param is optional and can be left blank (0x0000), if done so, the best fitting mechanism will be chosen automatically.</param>
        /// <returns>Returns true on success, false otherwise.</returns>
        public bool Authenticate(NetworkCredential credentials, SaslMechanics mechanic = (SaslMechanics) 0x0000)
        {
            var authenticator = new ImapAuthenticator(this);
            if (mechanic != 0x0000) {
                IsAuthenticated = authenticator.Authenticate(credentials, mechanic);
                return IsAuthenticated;
            }

            IsAuthenticated = authenticator.CanAuthenticate
                                  ? authenticator.Authenticate(credentials)
                                  : InvokeManualSaslAuthenticationRequired(credentials, this);

            return IsAuthenticated;
        }

        /// <summary>
        ///   The EXAMINE command is identical to SELECT and returns the same output; however, the selected mailbox is identified as read-only.
        ///   http://tools.ietf.org/html/rfc3501#section-6.3.2
        /// </summary>
        /// <param name = "mailboxName">The name of the mailbox to select.</param>
        public SelectExamineImapResponse Examine(string mailboxName)
        {
            // we need to convert non ASCII names according to IMAP specs.
            // http://tools.ietf.org/html/rfc2060#section-5.1.3
            var name = MailboxNameEncoder.Encode(mailboxName);

            var text = string.Format("EXAMINE {0}", name);
            var command = new ImapCommand(text);
            var reader = SendAndReceive(command, false);

            var response = new SelectExamineImapResponse(mailboxName);
            response.Parse(reader);

            if (response.IsOk) {
                SelectedMailbox = mailboxName;
            }

            return response;
        }

        /// <summary>
        ///   The CLOSE command permanently removes all messages that have the 
        ///   \Deleted flag set from the currently selected mailbox, and returns to the authenticated state from the selected state.
        /// </summary>
        /// <param name = "mailboxName">The targeted mailboxName.</param>
        public ImapResponse Close(string mailboxName)
        {
            // we need to convert non ASCII names according to IMAP specs.
            // http://tools.ietf.org/html/rfc2060#section-5.1.3
            var name = MailboxNameEncoder.Encode(mailboxName);

            var text = string.Format("CLOSE {0}", name);
            var command = new ImapCommand(text);
            var reader = SendAndReceive(command);

            var response = new ImapResponse();
            response.Parse(reader);

            if (response.IsOk) {
                SelectedMailbox = string.Empty;
            }

            return response;
        }

        /// <summary>
        ///   The SEARCH command searches the mailbox for messages that match
        ///   the given searching criteria.  Searching criteria consist of one
        ///   or more search keys.
        /// </summary>
        /// <param name = "query">The query command that will be sent to the server.</param>
        /// <param name = "isUidSearch">Sets whether the search shall return uids instead of sequence numbers.</param>
        /// <returns>The sequence numbers or uids of those messages matching the given criteria.</returns>
        public SearchImapResponse Search(string query, bool isUidSearch = false)
        {
            var uid = isUidSearch ? "UID " : string.Empty;
            var text = string.Format("{0}SEARCH {1}", uid, query);
            var command = new ImapCommand(text);
            var reader = SendAndReceive(command);
            var response = new SearchImapResponse();
            response.Parse(reader);
            return response;
        }

        /// <summary>
        ///   The CHECK command requests a checkpoint of the currently selected
        ///   mailbox.  A checkpoint refers to any implementation-dependent
        ///   housekeeping associated with the mailbox (e.g., resolving the
        ///   server's in-memory state of the mailbox with the state on its
        ///   disk) that is not normally executed as part of each command.  A
        ///   checkpoint MAY take a non-instantaneous amount of real time to
        ///   complete.  If a server implementation has no such housekeeping
        ///   considerations, CHECK is equivalent to NOOP.
        ///   http://tools.ietf.org/html/rfc2060#section-6.4.1
        /// </summary>
        public ImapResponse Check()
        {
            var text = string.Format("CHECK");
            var command = new ImapCommand(text);
            var reader = SendAndReceive(command);

            var response = new ImapResponse();
            response.Parse(reader);
            return response;
        }

        /// <summary>
        ///   The SELECT command selects a mailbox so that messages in the mailbox can be accessed.
        ///   http://tools.ietf.org/html/rfc3501#section-6.3.1
        /// </summary>
        /// <param name = "mailboxName">The name of the mailbox to select.</param>
        public SelectExamineImapResponse Select(string mailboxName)
        {
            // we need to convert non ASCII names according to IMAP specs.
            // http://tools.ietf.org/html/rfc2060#section-5.1.3
            var name = MailboxNameEncoder.Encode(mailboxName);

            var text = string.Format("SELECT \"{0}\"", name);
            var command = new ImapCommand(text);
            var reader = SendAndReceive(command, false);

            var response = new SelectExamineImapResponse(mailboxName);
            response.Parse(reader);

            if (response.IsOk) {
                SelectedMailbox = mailboxName;
            }

            return response;
        }

        /// <summary>
        ///   The EXPUNGE command permanently removes all messages that have the 
        ///   \Deleted flag set from the currently selected mailbox.  Before returning 
        ///   an OK to the client, an untagged EXPUNGE response is sent for each message that is removed.
        /// </summary>
        public ExpungeImapResponse Expunge()
        {
            var text = string.Format("EXPUNGE");
            var command = new ImapCommand(text);
            var reader = SendAndReceive(command);

            var response = new ExpungeImapResponse();
            response.Parse(reader);
            return response;
        }

        /// <summary>
        ///   The DELETE command permanently removes the mailbox with the given name.
        ///   http://tools.ietf.org/html/rfc3501#section-6.3.4
        /// </summary>
        /// <param name = "mailboxName">The name of the mailbox to delete.</param>
        public ImapResponse Delete(string mailboxName)
        {
            // we need to convert non ASCII names according to IMAP specs.
            // http://tools.ietf.org/html/rfc2060#section-5.1.3
            var name = MailboxNameEncoder.Encode(mailboxName);

            var text = string.Format("DELETE \"{0}\"", name);
            var command = new ImapCommand(text);
            var reader = SendAndReceive(command);

            var response = new ImapResponse();
            response.Parse(reader);
            return response;
        }

        /// <summary>
        ///   The STORE command alters data associated with a message in the mailbox.
        ///   http://tools.ietf.org/html/rfc2060#section-6.4.6
        /// </summary>
        /// <param name = "set">The sequence set representing the targeted messages, e.g. "1"; "1,2"; "2:4".</param>
        /// <param name = "value">The keyword to add or remove.</param>
        /// <param name = "procedure">The procedure, whether to add or remove the flags.</param>
        public StoreImapResponse Store(SequenceSet set, string value, StoreProcedures procedure)
        {
            var reader = StoreInternal(set, value, procedure, "FLAGS");
            var response = new StoreImapResponse();
            response.Parse(reader);
            return response;
        }

        /// <summary>
        ///   The STORE command alters data associated with a message in the mailbox.
        ///   http://tools.ietf.org/html/rfc2060#section-6.4.6
        /// </summary>
        /// <param name = "set">The sequence set representing the targeted messages, e.g. "1"; "1,2"; "2:4".</param>
        /// <param name = "flags">The flags to add or remove.</param>
        /// <param name = "procedure">The procedure, whether to add or remove the flags.</param>
        public StoreImapResponse Store(SequenceSet set, MessageFlags flags, StoreProcedures procedure)
        {
            var value = flags.ToMimeFormat();
            return Store(set, value, procedure);
        }

        /// <summary>
        ///   The STORE command alters data associated with a message in the mailbox.
        ///   http://tools.ietf.org/html/rfc2060#section-6.4.6
        /// </summary>
        /// <param name = "set">The sequence set representing the targeted messages, e.g. "1"; "1,2"; "2:4".</param>
        /// <param name = "flags">The flags to add or remove.</param>
        /// <param name = "procedure">The procedure, whether to add or remove the flags.</param>
        public ImapResponse StoreSilent(SequenceSet set, MessageFlags flags, StoreProcedures procedure)
        {
            var value = flags.ToMimeFormat();
            return StoreSilent(set, value, procedure);
        }

        /// <summary>
        ///   The STORE command alters data associated with a message in the mailbox.
        ///   http://tools.ietf.org/html/rfc2060#section-6.4.6
        /// </summary>
        /// <param name = "set">The sequence set representing the targeted messages, e.g. "1"; "1,2"; "2:4".</param>
        /// <param name = "value">The keywords to add or remove.</param>
        /// <param name = "procedure">The procedure, whether to add or remove the flags.</param>
        public ImapResponse StoreSilent(SequenceSet set, string value, StoreProcedures procedure)
        {
            var reader = StoreInternal(set, value, procedure, "FLAGS.SILENT");
            var response = new ImapResponse();
            response.Parse(reader);
            return response;
        }

        private ImapResponseReader StoreInternal(SequenceSet set, string value, StoreProcedures procedure, string commandString)
        {
            var prefix = procedure == StoreProcedures.Add ? "+" : "-";
            var isUid = set.IsUid ? "UID " : string.Empty;
            var text = string.Format("{0}STORE {1} {2}{3} ({4})", isUid, set, prefix, commandString, value);
            var command = new ImapCommand(text);
            return SendAndReceive(command, false);
        }

        /// <summary>
        ///   The NOOP command always succeeds.  It does nothing.
        ///   http://tools.ietf.org/html/rfc3501#section-6.1.2
        /// </summary>
        public ImapResponse Noop()
        {
            var command = new ImapCommand("NOOP");
            var reader = SendAndReceive(command);

            var response = new ImapResponse();
            response.Parse(reader);
            return response;
        }

        /// <summary>
        ///   The COPY command copies the specified message(s) to the end of the specified destination mailbox.
        ///   http://tools.ietf.org/html/rfc3501#section-6.4.7
        /// </summary>
        public ImapResponse Copy(SequenceSet set, string destinationMailboxName)
        {
            // we need to convert non ASCII names according to IMAP specs.
            // http://tools.ietf.org/html/rfc2060#section-5.1.3
            var name = MailboxNameEncoder.Encode(destinationMailboxName);

            var text = string.Format("COPY {0} \"{1}\"", set, name);
            var command = new ImapCommand(text);
            var reader = SendAndReceive(command);

            var response = new ImapResponse();
            response.Parse(reader);
            return response;
        }

        /// <summary>
        ///   The SUBSCRIBE command adds the specified mailbox name to the
        ///   server's set of "active" or "subscribed" mailboxes as returned by
        ///   the LSUB command.
        ///   http://tools.ietf.org/html/rfc3501#section-6.3.6
        /// </summary>
        /// <param name = "mailboxName">The name of the mailbox to subscribe to.</param>
        public ImapResponse Subscribe(string mailboxName)
        {
            // we need to convert non ASCII names according to IMAP specs.
            // http://tools.ietf.org/html/rfc2060#section-5.1.3
            var name = MailboxNameEncoder.Encode(mailboxName);

            var text = string.Format("SUBSCRIBE \"{0}\"", name);
            var command = new ImapCommand(text);
            var reader = SendAndReceive(command);

            var response = new ImapResponse();
            response.Parse(reader);
            return response;
        }

        /// <summary>
        ///   The UNSUBSCRIBE command removes the specified mailbox name from
        ///   the server's set of "active" or "subscribed" mailboxes as returned
        ///   by the LSUB command.
        ///   http://tools.ietf.org/html/rfc3501#section-6.3.7
        /// </summary>
        /// <param name = "mailboxName">The name of the mailbox to subscribe to.</param>
        public ImapResponse Unsubscribe(string mailboxName)
        {
            // we need to convert non ASCII names according to IMAP specs.
            // http://tools.ietf.org/html/rfc2060#section-5.1.3
            var name = MailboxNameEncoder.Encode(mailboxName);

            var text = string.Format("UNSUBSCRIBE \"{0}\"", name);
            var command = new ImapCommand(text);
            var reader = SendAndReceive(command);

            var response = new ImapResponse();
            response.Parse(reader);
            return response;
        }

        /// <summary>
        ///   The LOGOUT command informs the server that the client is done with the connection.
        ///   http://tools.ietf.org/html/rfc3501#section-6.1.3
        /// </summary>
        public ImapResponse Logout()
        {
            var command = new ImapCommand("LOGOUT");
            var reader = SendAndReceive(command);

            var response = new ImapResponse();
            response.Parse(reader);
            return response;
        }

        /// <summary>
        ///   The CREATE command creates a mailbox with the given name.
        ///   http://tools.ietf.org/html/rfc3501#section-6.3.3
        /// </summary>
        /// <param name = "mailboxName">The name of the mailbox to create.</param>
        public ImapResponse Create(string mailboxName)
        {
            // we need to convert non ASCII names according to IMAP specs.
            // http://tools.ietf.org/html/rfc2060#section-5.1.3
            var name = MailboxNameEncoder.Encode(mailboxName);

            var text = string.Format("CREATE \"{0}\"", name);
            var command = new ImapCommand(text);
            var reader = SendAndReceive(command);

            var response = new ImapResponse();
            response.Parse(reader);
            return response;
        }

        /// <summary>
        ///   The RENAME command changes the name of a mailbox.  A tagged OK
        ///   response is returned only if the mailbox has been renamed.  It is
        ///   an error to attempt to rename from a mailbox name that does not
        ///   exist or to a mailbox name that already exists.  Any error in
        ///   renaming will return a tagged NO response.
        /// </summary>
        /// <param name = "sourceName">The fullname of the mailbox to rename.</param>
        /// <param name = "targetName">The new name for the mailbox.</param>
        /// <returns>Returns the server response.</returns>
        public ImapResponse Rename(string sourceName, string targetName)
        {
            // we need to convert non ASCII names according to IMAP specs.
            // http://tools.ietf.org/html/rfc2060#section-5.1.3
            var source = MailboxNameEncoder.Encode(sourceName);

            // we need to convert non ASCII names according to IMAP specs.
            // http://tools.ietf.org/html/rfc2060#section-5.1.3
            var target = MailboxNameEncoder.Encode(targetName);

            var text = string.Format("RENAME \"{0}\" \"{1}\"", source, target);
            var command = new ImapCommand(text);
            var reader = SendAndReceive(command);

            var response = new ImapResponse();
            response.Parse(reader);
            return response;
        }

        /// <summary>
        ///   The APPEND command appends the literal argument as a new message
        ///   to the end of the specified destination mailbox.
        ///   http://tools.ietf.org/html/rfc3501#section-6.3.11
        /// </summary>
        /// <param name = "mailboxName">The name of the malbox to insert the message.</param>
        /// <param name = "message">The message to append.</param>
        /// <param name = "flags">Sets the flags of the message. This is optional.</param>
        public ImapResponse Append(string mailboxName, Message message, MessageFlags flags = (MessageFlags) 0x0000)
        {
            // we need to convert non ASCII names according to IMAP specs.
            // http://tools.ietf.org/html/rfc2060#section-5.1.3
            var name = MailboxNameEncoder.Encode(mailboxName);

            var mime = message.ToMime();
            var size = mime.Length;
            var flagString = flags == 0x0000 ? string.Empty : string.Format(" ({0})", flags.ToMimeFormat());
            var text = string.Format("APPEND {0}{1} {{{2}}}", name, flagString, size);
            var reader = SendAndReceive(new ImapCommand(text));

            if (reader.IsContinuation) {
                var finalReader = SendAndReceive(new BlankImapCommand(mime));

                var validResponse = new ImapResponse();
                validResponse.Parse(finalReader);
                return validResponse;
            }

            var invalidResponse = new ImapResponse();
            invalidResponse.Parse(reader);
            return invalidResponse;
        }

        /// <summary>
        ///   The FETCH command retrieves data associated with a message in the
        ///   mailbox.  The data items to be fetched can be either a single atom
        ///   or a parenthesized list.
        ///   http://tools.ietf.org/html/rfc3501#section-6.4.5
        /// </summary>
        /// <param name = "set">The list of ids of the messages to fetch.</param>
        /// <param name = "query">The query, consisting of message parts to fetch.</param>
        public FetchImapResponse Fetch(SequenceSet set, string query)
        {
            var text = string.Format("FETCH {0} {1}", set, query);
            var command = new ImapCommand(text);
            var reader = SendAndReceive(command);
            var response = new FetchImapResponse();

            response.Parse(reader);
            return response;
        }

        /// <summary>
        ///   The LIST command returns a subset of names from the complete set of all names available to the client.
        ///   http://tools.ietf.org/html/rfc3501#section-6.3.8
        /// </summary>
        /// <param name = "referenceName">The reference name.</param>
        /// <param name = "wildcardedMailboxName">The mailbox name with possible wildcards.</param>
        public ListImapResponse List(string referenceName, string wildcardedMailboxName)
        {
            // we need to convert non ASCII names according to IMAP specs.
            // http://tools.ietf.org/html/rfc2060#section-5.1.3
            referenceName = MailboxNameEncoder.Encode(referenceName);
            wildcardedMailboxName = MailboxNameEncoder.Encode(wildcardedMailboxName);

            var text = string.Format("LIST \"{0}\" \"{1}\"", referenceName, wildcardedMailboxName);
            var command = new ImapCommand(text);
            var reader = SendAndReceive(command);

            var response = new ListImapResponse();
            response.Parse(reader);
            return response;
        }

        /// <summary>
        ///   The LSUB command returns a subset of names from the set of names that the user has declared as being "active" or "subscribed".
        ///   http://tools.ietf.org/html/rfc3501#section-6.3.9
        /// </summary>
        /// <param name = "referenceName">The reference name.</param>
        /// <param name = "wildcardedMailboxName">The mailbox name with possible wildcards.</param>
        public ListImapResponse LSub(string referenceName, string wildcardedMailboxName)
        {
            // we need to convert non ASCII names according to IMAP specs.
            // http://tools.ietf.org/html/rfc2060#section-5.1.3
            referenceName = MailboxNameEncoder.Encode(referenceName);
            wildcardedMailboxName = MailboxNameEncoder.Encode(wildcardedMailboxName);

            var text = string.Format("LSUB \"{0}\" \"{1}\"", referenceName, wildcardedMailboxName);
            var command = new ImapCommand(text);
            var reader = SendAndReceive(command);

            var response = new ListImapResponse();
            response.Parse(reader);
            return response;
        }

        /// <summary>
        /// Disposes of the client object.
        /// In addition calling this method will trigger an autoomated logout and disconnect from the server.
        /// </summary>
        public override void Dispose()
        {
            if (IsAuthenticated) {
                Logout();
            }

            base.Dispose();
        }

        #region IDLE Command

        internal void ReceiveIdleStatusUpdates(ImapResponseReader reader)
        {
            reader.ReadNextLine();

            if (reader.IsCompleted) {
                return;
            }

            StopIdleAsync();

            var e = new StatusUpdateReceivedEventArgs(SelectedMailbox) {IsIdleUpdate = true};
            while (true) {
                if (reader.IsCompleted) {
                    InvokeStatusUpdateReceived(e);
                    break;
                }

                if (reader.IsStatusUpdate) {
                    e.MailboxInfo.InjectLine(reader.CurrentLine);
                }

                // We must check seperately for FETCH updates since we cannot include them into regular status update checks.
                // If included they would be processed as a status update and not as a FETCH response, thus corrupting the stack.
                // We can only check for this kind of update inside the IDLE loop, since we know here that no previous FETCH command has been issued and
                // it is therefor safe to process a FETCH update response.
                if (reader.CurrentLine.Contains("FETCH")) {
                    if (e.MailboxInfo.MessageStateChanges == null) {
                        e.MailboxInfo.MessageStateChanges = new List<MessageState>();
                    }
                    var state = new MessageState();
                    state.InjectLine(reader.CurrentLine);
                    ((IList<MessageState>) e.MailboxInfo.MessageStateChanges).Add(state);
                }

                reader.ReadNextLine();
            }

            if (!e.IsIdleCancelled) {
                StartIdle();
            }
        }

        /// <summary>
        ///   This method is blocking.
        ///   The IDLE command may be used with any IMAP4 server implementation
        ///   that returns "IDLE" as one of the supported capabilities to the
        ///   CAPABILITY command.  If the server does not advertise the IDLE
        ///   capability, the client MUST NOT use the IDLE command and must poll
        ///   for mailbox updates.
        ///   http://tools.ietf.org/html/rfc2177
        /// </summary>
        public void StartIdle()
        {
            if (!ServerCapability.IsIdleSupported) {
                const string message = "Server does not support the idle command. Please check Capability.CanIdle before calling Idle.";
                throw new InvalidOperationException(message);
            }

            var command = new ImapCommand("IDLE");
            SendAndReceive(command);
            IsIdling = true;

            while (true) {
                // Need to set response timeout to 29 minutes, because the server will kick us after 30  if we do not re apply for IDLE.
                ResponseTimeout = TimeSpan.FromMinutes(29);
                var reader = new ImapResponseReader(this);
                try {
                    ReceiveIdleStatusUpdates(reader);
                }
                catch (TimeoutException) {
                    StopIdleAsync();
                }

                if (reader.IsCompleted) {
                    break;
                }
            }

            IsIdling = false;
        }

        /// <summary>
        ///   Send the DONE command to the server on a seperate thread, which will release the IDLE lock.
        /// </summary>
        public void StopIdleAsync()
        {
            var command = new BlankImapCommand("DONE");
            SendAsync(command);
        }

        #endregion

        #region Nested Classes

        private sealed class MessageContainer
        {
            public int SequenceNumber { get; set; }
            public string Text { get; set; }
            public int Uid { get; set; }
        }

        #endregion

        #region Fetch Commands

        /// <summary>
        /// Fetches a single message from the selected mailbox identified by its sequence number.
        /// </summary>
        /// <param name="sn">The sequence number of the requested message.</param>
        /// <returns>The requested message.</returns>
        public Message FetchMessageBySequenceNumber(int sn)
        {
            var query = Messages
                .Where(x => x.SequenceNumber == sn)
                .Select(x => new MessageContainer {
                                     Uid = x.Uid,
                                     SequenceNumber = x.SequenceNumber,
                                     Text = (string) x.Parts(string.Empty)
                                 });
            var container = query.ToList().FirstOrDefault();
            if (container != null) {
                var entity = new Entity();
                entity.Deserialize(container.Text);

                var message = entity.ToMessage();
                message.Uid = container.Uid;
                message.SequenceNumber = container.SequenceNumber;
                return message;
            }

            return null;
        }

        /// <summary>
        /// Fetches a single message from the selected mailbox identified by its unique identifier.
        /// </summary>
        /// <param name="uid">The uid of the requested message.</param>
        /// <returns>The requested message.</returns>
        public Message FetchMessageByUid(int uid)
        {
            var query = Messages
                .Where(x => x.Uid == uid)
                .Select(x => new MessageContainer {
                                     Uid = x.Uid,
                                     SequenceNumber = x.SequenceNumber,
                                     Text = (string) x.Parts(string.Empty)
                                 });
            var container = query.ToList().FirstOrDefault();
            if (container != null) {
                var entity = new Entity();
                entity.Deserialize(container.Text);

                var message = entity.ToMessage();
                message.Uid = container.Uid;
                message.SequenceNumber = container.SequenceNumber;
                return message;
            }

            return null;
        }

        /// <summary>
        /// Fetches a single attachment from the requested message.
        /// </summary>
        /// <param name="info">The associated info object, this token can be obtained from the messages body structure.</param>
        /// <returns>The requested attachment.</returns>
        public Attachment FetchAttachment(AttachmentInfo info)
        {
            var base64String = FetchEntityText(info);
            if (string.IsNullOrEmpty(base64String)) {
                return null;
            }
            var bytes = Convert.FromBase64String(base64String);
            return Attachment.FromBytes(info.Name, bytes, info.MediaType);
        }

        /// <summary>
        /// Fetches a single nested message from the requested message.
        /// </summary>
        /// <param name="info">The associated info object, this token can be obtained from the messages body structure.</param>
        /// <returns>The requested nested message.</returns>
        public Message FetchNestedMessage(MessageInfo info)
        {
            var text = FetchEntityText(info);
            if (string.IsNullOrEmpty(text)) {
                return null;
            }
            var entity = new Entity();
            entity.Deserialize(text);
            return entity.ToMessage();
        }

        private string FetchEntityText(IFetchable fetchable)
        {
            var token = fetchable.Token;
            var query = Messages.Where(x => x.SequenceNumber == fetchable.SequenceNumber).Select(x => x.Parts(token)).ToList().FirstOrDefault();
            var text = (string) query;
            if (string.IsNullOrEmpty(text)) {
                Debug.WriteLine("Unable to fetch item, server did not give any response. Perhaps it was removed ?");
                return string.Empty;
            }
            return text;
        }

        private string FetchEntityBody(IFetchable fetchable)
        {
            var token = fetchable.Token;
            var query = Messages.Where(x => x.SequenceNumber == fetchable.SequenceNumber).Select(x => x.Text).ToList().FirstOrDefault();
            var text = (string)query;
            text = query.Replace("\r\n", "<br />");
            if (string.IsNullOrEmpty(text))
            {
                Debug.WriteLine("Unable to fetch item, server did not give any response. Perhaps it was removed ?");
                return string.Empty;
            }
            return text;
        }

        /// <summary>
        /// Fetches a single view from the requested message.
        /// </summary>
        /// <param name="info">The associated info object, this token can be obtained from the messages body structure.</param>
        /// <returns>The requested view.</returns>
        public View FetchView(ViewInfo info)
        {
            var text = FetchEntityText(info);
            if (string.IsNullOrEmpty(text)) {
                return null;
            }
            var paramDictionary = ((IDictionary<string, string>)info.Parameters);
            var hasValidCharset = paramDictionary.ContainsKey("charset");
            var charset = hasValidCharset ? paramDictionary["charset"] : Charsets.Utf8;
            var decoded = TransferEncoder.Decode(text, info.ContentTransferEncoding, charset);

            return new View {MediaType = info.MediaType, Text = decoded};
        }

        /// <summary>
        /// Fetches a single view from the requested message.
        /// </summary>
        /// <param name="info">The associated info object, this token can be obtained from the messages body structure.</param>
        /// <returns>The requested view.</returns>
        public View FetchView2(ViewInfo info)
        {
            var text = FetchEntityBody(info);
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }
            var paramDictionary = ((IDictionary<string, string>)info.Parameters);
            var hasValidCharset = paramDictionary.ContainsKey("charset");
            var charset = hasValidCharset ? paramDictionary["charset"] : Charsets.Utf8;
            var decoded = TransferEncoder.Decode(text, info.ContentTransferEncoding, charset);

            return new View { MediaType = info.MediaType, Text = decoded };
        }

        #endregion
    }
}