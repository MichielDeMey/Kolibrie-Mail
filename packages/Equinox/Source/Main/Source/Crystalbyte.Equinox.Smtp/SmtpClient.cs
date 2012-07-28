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
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Crystalbyte.Equinox.Security;
using Crystalbyte.Equinox.Security.Authentication;
using Crystalbyte.Equinox.Smtp.Responses;

namespace Crystalbyte.Equinox.Smtp
{
    /// <summary>
    ///   An SMTP client. 
    ///   http://tools.ietf.org/html/rfc5321
    /// </summary>
    public sealed class SmtpClient : SecureClient
    {
        private readonly UploadProgressChangedEventArgs _uploadProgressChangedEventArgs;

        public SmtpClient()
        {
            Security = SecurityPolicies.None;
            ResponseTimeout = TimeSpan.FromMinutes(5);
            UpdateUploadProgressTriggerChunkSize = Size.FromBytes(500);
            _uploadProgressChangedEventArgs = new UploadProgressChangedEventArgs(0, 1);
        }

        public bool IsAuthenticated { get; private set; }
        public SmtpServerCapability ServerCapabilities { get; private set; }
        public TimeSpan ResponseTimeout { get; set; }

        /// <summary>
        ///   This property sets the recurring chunk size after which a upload progress update event is being thrown.
        ///   As default every 500 bytes the update is being triggered.
        /// </summary>
        internal Size UpdateUploadProgressTriggerChunkSize { get; set; }

        internal SmtpResponseReader SendAndReceive(SmtpCommand command)
        {
            Send(command);
            return Receive();
        }

        public event EventHandler<UploadProgressChangedEventArgs> UploadProgressChanged;

        private void InvokeUploadProgressChanged(int current, int total)
        {
            var handler = UploadProgressChanged;
            if (handler != null) {
                _uploadProgressChangedEventArgs.Current = current;
                _uploadProgressChangedEventArgs.Total = total;
                handler(this, _uploadProgressChangedEventArgs);
            }
        }

        public event EventHandler<ManualSaslAuthenticationRequiredEventArgs> ManualSaslAuthenticationRequired;

        /// <summary>
        ///   Attempt to notify the application that automatic sasl authentication is not possible.
        ///   The ManualSaslAuthenticationRequired event wil be thrown.
        /// </summary>
        /// <param name = "credential">The credentials provided by the user.</param>
        /// <param name = "client">The current instance of the client, responsiblle for the connection.</param>
        /// <returns>Returns whether the client should cancel the authentication request.</returns>
        private bool InvokeManualSaslAuthenticationRequired(NetworkCredential credential, SmtpClient client)
        {
            var handler = ManualSaslAuthenticationRequired;
            if (handler != null) {
                var e = new ManualSaslAuthenticationRequiredEventArgs(credential, client);
                handler(this, e);
                return e.IsAuthenticated;
            }
            return false;
        }

        public bool AuthenticateXOAuth(string key)
        {
            IsAuthenticated = new SmtpAuthenticator(this).AuthenticateXOAuth(key);
            return IsAuthenticated;
        }

        public bool Authenticate(string username, string password, SaslMechanics mechanic = (SaslMechanics) 0x0000)
        {
            return Authenticate(new NetworkCredential(username, password), mechanic);
        }

        public bool Authenticate(NetworkCredential credentials, SaslMechanics mechanic = (SaslMechanics) 0x0000)
        {
            var authenticator = new SmtpAuthenticator(this);

            if (mechanic != 0x0000) {
                IsAuthenticated = authenticator.Authenticate(credentials, mechanic);
                return IsAuthenticated;
            }

            IsAuthenticated = authenticator.CanAuthenticate ?
                                                                authenticator.Authenticate(credentials) :
                                                                                                            InvokeManualSaslAuthenticationRequired(credentials, this);
            return IsAuthenticated;
        }

        public override void Dispose()
        {
            Quit();
            base.Dispose();
        }

        public SmtpResponseReader Send(Message message)
        {
            if (!message.To.Any()) {
                const string msg = "Message cannot be sent without recipients. Please use the 'To' property to add contacts to the destination list.";
                throw new ApplicationException(msg);
            }

            if (!message.From.Any())
            {
                const string msg = "Message cannot be sent without senders. Please use the 'From' property to add contacts to the source list.";
                throw new ApplicationException(msg);
            }

            var reader = SendSenders(message);
            if (reader != null && reader.IsFatalOrErroneous) {
                return reader;
            }

            reader = SendRecipients(message);
            if (reader == null || reader.IsFatalOrErroneous) {
                return reader;
            }

            return SendData(message);
        }

        public VerifySmtpResponse Verify(string userName)
        {
            var message = string.Format("VRFY \"{0}\"", userName);
            var reader = SendAndReceive(new SmtpCommand(message));
            var response = new VerifySmtpResponse();
            response.Parse(reader);
            return response;
        }

        public SmtpResponseReader Quit()
        {
            return SendAndReceive(new SmtpCommand("QUIT"));
        }

        private SmtpResponseReader SendData(Message message)
        {
            SendAndReceive(new SmtpCommand("DATA"));

            var mime = message.ToMime();
            using (var reader = new StringReader(mime)) {
                var chunkSize = UpdateUploadProgressTriggerChunkSize.Bytes;
                var trigger = chunkSize;
                var current = 0;
                var total = mime.Length;

                var command = new SmtpCommand(string.Empty);
                while (true) {
                    var line = reader.ReadLine();
                    if (line == null) {
                        break;
                    }

                    command.Text = StuffPeriodIfNecessary(line);
                    Send(command);

                    // dont forget the newline characters
                    current += command.Text.Length + 2;

                    if (trigger - current < 0) {
                        InvokeUploadProgressChanged(current, total);
                        trigger += chunkSize;
                    }

                    if (current >= total) {
                        InvokeUploadProgressChanged(total, total);
                    }
                }
                SendDataTermination();
            }

            return Receive();
        }

        private void SendDataTermination()
        {
            var termination = Environment.NewLine + "." + Environment.NewLine;
            Send(new SmtpCommand(termination));
        }

        private static string StuffPeriodIfNecessary(string line)
        {
            if (line.StartsWith(".")) {
                line = line.Insert(0, ".");
            }
            return line;
        }

        private SmtpResponseReader SendSenders(Message message)
        {
            SmtpResponseReader reader = null;
            foreach (var from in message.From) {
                var text = string.Format("MAIL FROM:<{0}>", from.Address.FullAddress);
                var command = new SmtpCommand(text);
                reader = SendAndReceive(command);

                if (reader.IsFatalOrErroneous) {
                    return reader;
                }
            }
            return reader;
        }

        private SmtpResponseReader SendRecipients(Message message)
        {
            SmtpResponseReader reader = null;
            var recipients = new List<EmailContact>();
            recipients.AddRange(message.Ccs);
            recipients.AddRange(message.Bccs);
            recipients.AddRange(message.To);
            recipients.RemoveDuplicates();

            foreach (var recipient in recipients) {
                var text = string.Format("RCPT TO:<{0}>", recipient.Address.FullAddress);
                var command = new SmtpCommand(text);
                reader = SendAndReceive(command);

                if (reader.IsFatalOrErroneous) {
                    return reader;
                }
            }

            return reader;
        }

        /// <summary>
        ///   This command specifies that the current mail transaction will be
        ///   aborted.  Any stored sender, recipients, and mail data will be
        ///   discarded, and all buffers and state tables cleared.
        /// </summary>
        /// <returns>A ResponseReader.</returns>
        public SmtpResponseReader Reset()
        {
            return SendAndReceive(new SmtpCommand("RSET"));
        }

        private SmtpServerCapability Hello()
        {
            var host = Dns.GetHostName();
            var command = new SmtpCommand("EHLO " + host);
            Send(command);

            // Timeout suggestion 
            // http://tools.ietf.org/html/rfc5321#section-4.5.3.2.1
            var timeout = TimeSpan.FromMinutes(5);
            var response = Receive(timeout);

            // fallback to HELO if EHLO is not supported
            if (response.IsFatalOrErroneous) {
                command = new SmtpCommand("HELO " + host);
                Send(command);
                response = Receive(timeout);
            }

            return StaticResponseParser.ParseCapability(response);
        }

        internal SmtpResponseReader Receive(TimeSpan timeout)
        {
            var reader = new SmtpResponseReader(this, timeout);
            reader.ReadNextLine();
            return reader;
        }

        internal SmtpResponseReader Receive()
        {
            return Receive(ResponseTimeout);
        }

        internal string ReceiveLine()
        {
            var line = ReadLine();
            return line;
        }

        internal void Send(SmtpCommand command)
        {
            WriteLine(command.Text);
        }

        protected override void FetchCapabilities()
        {
            ServerCapabilities = Hello();
        }

        protected override bool IsTlsSupported()
        {
            return ServerCapabilities.IsTlsSupported;
        }

        protected override bool IssueStartTlsCommand(string host)
        {
            var response = SendAndReceive(new SmtpCommand("STARTTLS"));
            return response.IsOk;
        }
    }
}