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
using System.Net;
using Crystalbyte.Equinox.Security;
using Crystalbyte.Equinox.Security.Authentication;

namespace Crystalbyte.Equinox.Pop3
{
    /// <summary>
    ///   Implementation of a pop3 client.
    ///   POP3: http://tools.ietf.org/html/rfc1939
    ///   POP3S: http://www.faqs.org/rfcs/rfc2595.html
    /// </summary>
    public sealed class Pop3Client : SecureClient
    {
        public Pop3ServerCapability ServerCapability { get; private set; }
        public bool IsAuthenticated { get; private set; }

        public event EventHandler<ManualSaslAuthenticationRequiredEventArgs> ManualSaslAuthenticationRequired;

        /// <summary>
        ///   Attempt to notify the application that automatic sasl authentication is not possible.
        ///   The ManualSaslAuthenticationRequired event wil be thrown.
        /// </summary>
        /// <param name = "credential">The credentials provided by the user.</param>
        /// <param name = "client">The current instance of the client, responsiblle for the connection.</param>
        /// <returns>Returns whether the client should cancel the authentication request.</returns>
        private bool InvokeManualSaslAuthenticationRequired(NetworkCredential credential, Pop3Client client)
        {
            var handler = ManualSaslAuthenticationRequired;
            if (handler != null) {
                var e = new ManualSaslAuthenticationRequiredEventArgs(credential, client);
                handler(this, e);
                return e.IsAuthenticated;
            }
            return false;
        }

        protected override void FetchCapabilities()
        {
            var command = new Pop3Command("CAPA");
            var reader = SendAndReceive(command);
            ServerCapability = reader.ReadCapability();
        }

        public bool Authenticate(string username, string password, SaslMechanics mechanic = (SaslMechanics) 0x0000)
        {
            return Authenticate(new NetworkCredential(username, password));
        }

        public bool Authenticate(NetworkCredential credentials, SaslMechanics mechanic = (SaslMechanics) 0x0000)
        {
            Arguments.VerifyNotNull(credentials);

            var authenticator = new Pop3Authenticator(this);
            if (mechanic != 0x0000) {
                IsAuthenticated = authenticator.Authenticate(credentials, mechanic);
                return IsAuthenticated;
            }

            IsAuthenticated = authenticator.CanAuthenticate
                                  ? authenticator.Authenticate(credentials)
                                  : InvokeManualSaslAuthenticationRequired(credentials, this);

            return IsAuthenticated;
        }

        public TopPop3Response Top(int number, int lines)
        {
            var text = string.Format("TOP {0} {1}", number, lines);
            var reader = SendAndReceive(new Pop3Command(text));
            var response = new TopPop3Response();
            response.ReadResponse(reader);
            return response;
        }

        public StatPop3Response Stat()
        {
            var reader = SendAndReceive(new Pop3Command("STAT"));
            var response = new StatPop3Response();
            response.ReadResponse(reader);
            return response;
        }

        public RetrPop3Response Retr(int number)
        {
            var text = string.Format("RETR {0}", number);
            var reader = SendAndReceive(new Pop3Command(text));
            var response = new RetrPop3Response();
            response.ReadResponse(reader);
            return response;
        }

        public Pop3Response Dele(int number)
        {
            var text = string.Format("DELE {0}", number);
            Send(new Pop3Command(text));
            return ReadDefaultResponse();
        }

        public Pop3Response Noop()
        {
            Send(new Pop3Command("NOOP"));
            return ReadDefaultResponse();
        }

        public ListPop3Response List()
        {
            return ListInternal(new Pop3Command("LIST"), false);
        }

        public Pop3Response Rset()
        {
            Send(new Pop3Command("RSET"));
            return ReadDefaultResponse();
        }

        public ListPop3Response List(int index)
        {
            var text = string.Format("LIST {0}", index);
            return ListInternal(new Pop3Command(text), true);
        }

        private ListPop3Response ListInternal(Pop3Command command, bool isSingle)
        {
            Arguments.VerifyNotNull(command);

            var reader = SendAndReceive(command);
            var response = new ListPop3Response(isSingle);
            response.ReadResponse(reader);
            return response;
        }

        public Pop3Response Quit()
        {
            Send(new Pop3Command("QUIT"));
            return ReadDefaultResponse();
        }

        private Pop3Response ReadDefaultResponse()
        {
            var reader = Receive();
            var response = new Pop3Response();
            response.ReadResponse(reader);
            return response;
        }

        public UidlPop3Response Uidl()
        {
            return UidlInternal(new Pop3Command("UIDL"), false);
        }

        public UidlPop3Response Uidl(int number)
        {
            var text = string.Format("UIDL {0}", number);
            return UidlInternal(new Pop3Command(text), true);
        }

        private UidlPop3Response UidlInternal(Pop3Command command, bool isSingle)
        {
            Arguments.VerifyNotNull(command);

            var reader = SendAndReceive(command);
            var response = new UidlPop3Response(isSingle);
            response.ReadResponse(reader);
            return response;
        }

        internal new string ReadLine()
        {
            return base.ReadLine();
        }

        public Pop3ResponseReader Receive()
        {
            var reader = new Pop3ResponseReader(this);
            reader.ReadNextLine();
            return reader;
        }

        protected override bool IsTlsSupported()
        {
            return ServerCapability.IsTlsSupported;
        }

        protected override bool IssueStartTlsCommand(string host)
        {
            Send(new Pop3Command("STLS"));
            return Receive().IsPositive;
        }

        public Pop3ResponseReader SendAndReceive(Pop3Command command)
        {
            Arguments.VerifyNotNull(command);

            Send(command);
            return Receive();
        }

        public void Send(Pop3Command command)
        {
            Arguments.VerifyNotNull(command);

            WriteLine(command.Text);
        }
    }
}