using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using Crystalbyte.Equinox.Imap;
using Crystalbyte.Equinox.Security.Authentication;
using Kolibrie_Mail.Model;

namespace Kolibrie_Mail.Protocols
{
    internal sealed class GmailAuthenticator
    {
        public void Authenticate(ImapClient client, NetworkCredential credential, Account account)
        {
            if (!client.ServerCapability.Items.Contains("AUTH=XOAUTH"))
            {
                return;
            }

            if (string.IsNullOrEmpty(account.XOAuthKey))
            {
                // in this case the username is the email address;
                var email = credential.UserName;

                var token = new OAuthRequest().WithAnonymousConsumer().WithEndpoint("https://www.google.com/accounts/OAuthGetRequestToken").WithParameter("scope", "https://mail.google.com/") // gmail specific
                    .WithParameter(OAuthParameters.OAuthCallback, "oob").WithParameter("xoauth_displayname", "Kolibrie Mail") // xoauth protocol specific
                    .WithSignatureMethod(OAuthSignatureMethods.HmacSha1).Sign().RequestToken();

                var authUrl = new OAuthRequest().WithEndpoint("https://www.google.com/accounts/OAuthAuthorizeToken").WithToken(token).GetAuthorizationUri();

                Process.Start(authUrl.AbsoluteUri);

                string verificationCode;
                using (var form = new OAuthVerificationForm())
                {
                    form.ShowDialog();
                    verificationCode = form.VerificationCode;
                    if (form.DialogResult.HasValue && !form.DialogResult.Value)
                    {
                        return;
                    }
                }

                var accessToken = new OAuthRequest().WithAnonymousConsumer().WithEndpoint("https://www.google.com/accounts/OAuthGetAccessToken").WithParameter(OAuthParameters.OAuthVerifier, verificationCode).WithSignatureMethod(OAuthSignatureMethods.HmacSha1).WithToken(token).Sign().RequestToken();

                var xOUrl = string.Format("https://mail.google.com/mail/b/{0}/imap/", email);
                account.XOAuthKey = new OAuthRequest().WithAnonymousConsumer().WithEndpoint(xOUrl).WithSignatureMethod(OAuthSignatureMethods.HmacSha1).WithToken(accessToken).Sign().CreateXOAuthKey();
                
                Console.WriteLine("XOAth key: " + account.XOAuthKey);
            }

            client.AuthenticateXOAuth(account.XOAuthKey);
        }
    }
}
