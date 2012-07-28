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
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Crystalbyte.Equinox.IO;
using Crystalbyte.Equinox.Net;
using Crystalbyte.Equinox.Text;

namespace Crystalbyte.Equinox.Security.Authentication
{
    public sealed class OAuthRequest
    {
        private const string _hmacSha1SignatureType = "HMAC-SHA1";
        private const string _rsaSha1SignatureType = "RSA-SHA1";
        private static readonly Random _random = new Random();
        private bool _sign;

        public OAuthRequest()
        {
            Parameters = new SortedDictionary<string, string>();
        }

        public OAuthConsumer Consumer { get; private set; }
        public OAuthSignatureMethods SignatureMethod { get; private set; }
        public Uri RequestEndpoint { get; private set; }
        public OAuthToken Token { get; private set; }
        public IDictionary<string, string> Parameters { get; private set; }

        public OAuthRequest WithAnonymousConsumer()
        {
            const string anonymous = "anonymous";
            Consumer = new OAuthConsumer {ConsumerKey = anonymous, ConsumerSecret = anonymous};
            return this;
        }

        public OAuthRequest WithToken(string token, string tokenSecret)
        {
            Token = new OAuthToken(token, tokenSecret);
            return this;
        }

        public OAuthRequest WithToken(OAuthToken token)
        {
            Token = token;
            Parameters.Add(OAuthParameters.OAuthToken, token.Value);
            return this;
        }

        public OAuthRequest WithConsumer(OAuthConsumer consumer)
        {
            Consumer = consumer;
            return this;
        }

        public OAuthRequest WithConsumer(string key, string secret)
        {
            Consumer = new OAuthConsumer {ConsumerKey = key, ConsumerSecret = secret};
            return this;
        }

        public OAuthRequest WithSignatureMethod(OAuthSignatureMethods method)
        {
            SignatureMethod = method;
            return this;
        }

        public OAuthRequest WithRequestUrl(string url)
        {
            RequestEndpoint = new Uri(url);
            return this;
        }

        public OAuthRequest WithParameter(string key, string value)
        {
            Parameters.Add(key, value);
            return this;
        }

        public string CreateXOAuthKey()
        {
            var requestParams = CreateOAuthRequestParams();

            if (_sign) {
                var signature = OAuthSignature.Create(this, requestParams);
                requestParams.Add(OAuthParameters.OAuthSignature, signature);
            }

            string token;
            using (var sw = new StringWriter()) {
                sw.Write("GET ");
                sw.Write(RequestEndpoint.AbsoluteUri);
                sw.Write(Characters.Space);

                var sorted = new SortedDictionary<string, string>();

                foreach (var p in Parameters) {
                    sorted.Add(p.Key, p.Value);
                }

                foreach (var p in requestParams) {
                    sorted.Add(p.Key, p.Value);
                }

                foreach (var p in sorted) {
                    sw.WriteFormat("{0}=\"{1}\",", p.Key, p.Value);
                }

                sw.TrimEnd(',');
                token = sw.ToString();
            }

            var bytes = Encoding.ASCII.GetBytes(token);
            return Convert.ToBase64String(bytes);
        }

        internal IDictionary<string, string> CreateOAuthRequestParams()
        {
            var methodString = SignatureMethod == OAuthSignatureMethods.HmacSha1 ? _hmacSha1SignatureType : _rsaSha1SignatureType;
            var @params = new SortedDictionary<string, string> {{OAuthParameters.OAuthConsumerKey, Consumer.ConsumerKey}, {OAuthParameters.OAuthSignatureMethod, methodString}, {OAuthParameters.OAuthTimestamp, GenerateTimeStamp()}, {OAuthParameters.OAuthVersion, "1.0"}};

            if (SignatureMethod != OAuthSignatureMethods.PlainText) {
                @params.Add(OAuthParameters.OAuthNonce, GenerateNonce());
            }

            return @params;
        }

        public OAuthRequest WithEndpoint(string url)
        {
            RequestEndpoint = new Uri(url);
            return this;
        }

        /// <summary>
        ///   Generates the timestamp for the signature
        /// </summary>
        /// <returns></returns>
        private static string GenerateTimeStamp()
        {
            // Default implementation of UNIX time of the current UTC time
            var ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds).ToString();
        }

        /// <summary>
        ///   Generate a nonce
        /// </summary>
        /// <returns></returns>
        private static string GenerateNonce()
        {
            // Just a simple implementation of a random number between 123400 and 9999999
            return _random.Next(123400, 9999999).ToString();
        }

        public OAuthRequest Sign()
        {
            _sign = true;
            return this;
        }

        public OAuthToken RequestToken()
        {
            var response = MakeRequest();
            using (var sr = new StreamReader(response.GetResponseStream())) {
                var content = sr.ReadToEnd();
                var values = content.Split(new[] {'&'});

                var token = new OAuthToken {Value = values.Where(x => x.StartsWith("oauth_token=")).First().Split('=')[1], Secret = values.Where(x => x.StartsWith("oauth_token_secret=")).First().Split('=')[1]};

                token.Value = HttpEncoder.UrlDecode(token.Value);
                token.Secret = HttpEncoder.UrlDecode(token.Secret);
                return token;
            }
        }

        public WebResponse MakeRequest()
        {
            var requestParams = CreateOAuthRequestParams();

            if (_sign) {
                var signature = OAuthSignature.Create(this, requestParams);
                requestParams.Add(OAuthParameters.OAuthSignature, signature);
            }

            string url;
            using (var sw = new StringWriter()) {
                sw.Write(RequestEndpoint.AbsoluteUri);
                sw.Write("?");

                var sorted = new SortedDictionary<string, string>();

                foreach (var p in Parameters) {
                    sorted.Add(p.Key, p.Value);
                }

                foreach (var p in requestParams) {
                    sorted.Add(p.Key, p.Value);
                }

                foreach (var p in sorted) {
                    sw.Write("{0}={1}", p.Key, p.Key == OAuthParameters.OAuthVerifier
                                                   ? HttpEncoder.UrlEncode(p.Value) : p.Value);

                    sw.Write("&");
                }
                // if params have been written the last item will be a '&' else it will be a '?'.
                sw.TrimEnd('&');
                sw.TrimEnd('?');

                url = sw.ToString();
            }

            var request = (HttpWebRequest) WebRequest.Create(url);
            request.Accept = "*/*";
            request.Method = "GET";
            request.Headers.Add(HttpRequestHeader.Authorization, "OAuth");
            return request.GetResponse();
        }

        public Uri GetAuthorizationUri()
        {
            string url;
            using (var sw = new StringWriter()) {
                sw.Write(RequestEndpoint.AbsoluteUri);
                if (Parameters.Count > 0) {
                    sw.Write("?");
                    foreach (var p in Parameters) {
                        sw.WriteFormat("{0}={1}&", p.Key, p.Value);
                    }
                    sw.TrimEnd('&');
                }
                url = sw.ToString();
            }

            var request = (HttpWebRequest) WebRequest.Create(url);
            request.Accept = "*/*";
            request.Method = "GET";

            var response = request.GetResponse();
            return response.ResponseUri;
        }
    }
}