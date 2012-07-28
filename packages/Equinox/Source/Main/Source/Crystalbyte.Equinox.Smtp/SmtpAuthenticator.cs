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
using Crystalbyte.Equinox.Security.Authentication;
using Crystalbyte.Equinox.Security.Cryptography;
using Crystalbyte.Equinox.Text;

namespace Crystalbyte.Equinox.Smtp
{
    public sealed class SmtpAuthenticator
    {
        private readonly SmtpClient _client;

        public SmtpAuthenticator(SmtpClient client)
        {
            _client = client;
        }

        public bool CanAuthenticate
        {
            get
            {
                var supported = _client.ServerCapabilities.GetSupportedSaslMechanics();
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

        public bool Authenticate(string username, string password, SaslMechanics mechanic = (SaslMechanics) 0x0000)
        {
            return Authenticate(new NetworkCredential(username, password), mechanic);
        }

        public bool Authenticate(NetworkCredential credentials, SaslMechanics mechanic = (SaslMechanics) 0x0000)
        {
            var supported = mechanic == 0x0000 ? _client.ServerCapabilities.GetSupportedSaslMechanics() : mechanic;

            var best = _client.IsSecure ? supported.GetFastest() : supported.GetSafest();

            switch (best) {
                case SaslMechanics.Plain:
                    return AuthenticatePlain(credentials);
                case SaslMechanics.Login:
                    return AuthenticateLogin(credentials);
                case SaslMechanics.CramMd5:
                    return AuthenticateCramMd5(credentials);
                default:
                    return false;
            }
        }

        /// <summary>
        ///   Plain authorization.
        ///   http://tools.ietf.org/html/rfc4616
        /// </summary>
        private bool AuthenticatePlain(NetworkCredential credentials)
        {
            var username = credentials.UserName;
            var password = credentials.Password;

            var auth = username + "\0" + username + "\0" + password;
            var encoded = Base64Encoder.Encode(auth);
            var command = string.Format("AUTH PLAIN {0}", encoded);
            return _client.SendAndReceive(new SmtpCommand(command)).IsOk;
        }

        /// <summary>
        ///   Cram-MD5 authorization.
        ///   http://tools.ietf.org/html/rfc2195
        /// </summary>
        private bool AuthenticateCramMd5(NetworkCredential credentials)
        {
            var command = new SmtpCommand("AUTH CRAM-MD5");
            var response = _client.SendAndReceive(command);

            var base64 = response.CurrentLine.Substring(4).TrimEnd();
            var challenge = Base64Encoder.Decode(base64, Encoding.UTF8);

            var username = credentials.UserName;
            var password = credentials.Password;

            var hash = CramMd5Hasher.ComputeHash(password, challenge);

            var authentication = username + " " + hash;
            var reader = _client.SendAndReceive(new SmtpCommand(Base64Encoder.Encode(authentication)));
            return reader.IsOk;
        }


        /// <summary>
        ///   Login authorization, same as plain but split into two separate command/responses.
        ///   http://tools.ietf.org/html/rfc4616
        /// </summary>
        private bool AuthenticateLogin(NetworkCredential credentials)
        {
            var command = new SmtpCommand("AUTH LOGIN");
            var response = _client.SendAndReceive(command);

            if (response.ResponseCode != 334) {
                return false;
            }

            var username = Base64Encoder.Encode(credentials.UserName);
            var userNameResponse = _client.SendAndReceive(new SmtpCommand(username));

            if (userNameResponse.ResponseCode != 334) {
                return false;
            }

            var password = Base64Encoder.Encode(credentials.Password);
            var passwordResponse = _client.SendAndReceive(new SmtpCommand(password));
            return passwordResponse.ResponseCode == ResponseCodes.AuthenticationSuccessful;
        }


        public bool AuthenticateXOAuth(string key)
        {
            var command = new SmtpCommand(string.Format("AUTH XOAUTH {0}", key));
            return _client.SendAndReceive(command).IsOk;
        }
    }
}