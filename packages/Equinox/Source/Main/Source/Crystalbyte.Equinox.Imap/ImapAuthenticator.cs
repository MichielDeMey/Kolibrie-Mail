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

using System.Net;
using System.Text;
using Crystalbyte.Equinox.Imap.Commands;
using Crystalbyte.Equinox.Security.Authentication;
using Crystalbyte.Equinox.Security.Cryptography;
using Crystalbyte.Equinox.Text;

namespace Crystalbyte.Equinox.Imap
{
    internal class ImapAuthenticator
    {
        private readonly ImapClient _client;

        public ImapAuthenticator(ImapClient client)
        {
            _client = client;
        }

        public bool CanAuthenticate
        {
            get
            {
                var supported = _client.ServerCapability.GetSupportedSaslMechanics();
                if (supported.HasFlag(SaslMechanics.Login)) {
                    return true;
                }

                if (supported.HasFlag(SaslMechanics.Plain)) {
                    return true;
                }

                if (supported.HasFlag(SaslMechanics.CramMd5)) {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        ///   The LOGIN command identifies the client to the server and carries the plain text password authenticating this user.
        ///   http://tools.ietf.org/html/rfc3501#section-6.2.3
        /// </summary>
        public bool Authenticate(NetworkCredential credentials, SaslMechanics mechanic = (SaslMechanics) 0x0000)
        {
            var supported = mechanic == 0x0000 ? _client.ServerCapability.GetSupportedSaslMechanics() : mechanic;

            var best = _client.IsSecure ? supported.GetFastest() : supported.GetSafest();

            switch (best) {
                case SaslMechanics.Login:
                    return AuthenticateLogin(credentials);
                case SaslMechanics.Plain:
                    return AuthenticatePlain(credentials);
                case SaslMechanics.CramMd5:
                    return AuthenticateCramMd5(credentials);
                default:
                    return false;
            }
        }

        private bool AuthenticateLogin(NetworkCredential credentials)
        {
            var text = string.Format("LOGIN {0} {1}", credentials.UserName, credentials.Password);
            var command = new ImapCommand(text);
            var reader = _client.SendAndReceive(command);
            while (!reader.IsCompleted) {
                reader = _client.Receive(false);
            }
            return reader.IsOk;
        }

        /// <summary>
        ///   Authenticates the client to the server using the XOAUTH mechanism.
        /// </summary>
        /// <param name = "key">The XOAUTH authetication key.</param>
        /// <returns>Returns true on success, false otherwise.</returns>
        public bool AuthenticateXOAuth(string key)
        {
            if (_client.ServerCapability.IsInitialClientResponseSupported) {
                var text = string.Format("AUTHENTICATE XOAUTH {0}", key);
                var command = new ImapCommand(text);
                return _client.SendAndReceive(command).IsOk;
            } else {
                var text = string.Format("AUTHENTICATE XOAUTH");
                var command = new ImapCommand(text);
                var reader = _client.SendAndReceive(command);
                if (reader.IsContinuation) {
                    var auth = new BlankImapCommand(key);
                    return _client.SendAndReceive(auth).IsOk;
                }
                return false;
            }
        }

        private bool AuthenticateCramMd5(NetworkCredential credentials)
        {
            var command = new ImapCommand("AUTHENTICATE CRAM-MD5");
            var response = _client.SendAndReceive(command);

            // don't trim the last plus !!
            var base64 = response.CurrentLine.TrimStart(Characters.Plus).Trim();
            var challenge = Base64Encoder.Decode(base64, Encoding.UTF8);

            var username = credentials.UserName;
            var password = credentials.Password;

            var hash = CramMd5Hasher.ComputeHash(password, challenge);

            var authentication = username + " " + hash;
            var authCommand = new BlankImapCommand(Base64Encoder.Encode(authentication));

            var reader = _client.SendAndReceive(authCommand);
            while (!reader.IsCompleted) {
                reader = _client.Receive(false);
            }
            return reader.IsOk;
        }

        private bool AuthenticatePlain(NetworkCredential credentials)
        {
            var capabilities = _client.ServerCapability;
            var username = credentials.UserName;
            var password = credentials.Password;

            var auth = username + "\0" + username + "\0" + password;
            var encodedAuth = Base64Encoder.Encode(auth);

            if (capabilities.IsInitialClientResponseSupported) {
                var text = string.Format("AUTHENTICATE PLAIN {0}", encodedAuth);
                var command = new ImapCommand(text);
                return _client.SendAndReceive(command).IsOk;
            }

            var authCommand = new ImapCommand("AUTHENTICATE PLAIN");
            var response = _client.SendAndReceive(authCommand);
            if (response.IsContinuation) {
                var command = new BlankImapCommand(encodedAuth);
                _client.Send(command);
                return _client.Receive().IsOk;
            }

            return false;
        }
    }
}