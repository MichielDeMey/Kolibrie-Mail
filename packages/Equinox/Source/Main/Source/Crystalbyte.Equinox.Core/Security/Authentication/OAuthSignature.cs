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
using System.Security.Cryptography;
using System.Text;
using Crystalbyte.Equinox.IO;
using Crystalbyte.Equinox.Net;

namespace Crystalbyte.Equinox.Security.Authentication
{
    public static class OAuthSignature
    {
        public const string HmacSha1SignatureType = "HMAC-SHA1";
        public const string PlainTextSignatureType = "PLAINTEXT";
        public const string RsaSha1SignatureType = "RSA-SHA1";

        public static string Create(OAuthRequest request, IDictionary<string, string> requestParams)
        {
            string signature;
            var ts = string.Empty;
            var cs = string.IsNullOrEmpty(request.Consumer.ConsumerSecret) ? string.Empty : HttpEncoder.UrlEncode(request.Consumer.ConsumerSecret);

            if (request.Token != null) {
                ts = string.IsNullOrEmpty(request.Token.Secret) ? string.Empty : HttpEncoder.UrlEncode(request.Token.Secret);
            }

            var key = string.Format("{0}&{1}", cs, ts);

            switch (request.SignatureMethod) {
                case OAuthSignatureMethods.PlainText:
                    signature = key;
                    break;
                case OAuthSignatureMethods.HmacSha1:
                    var url = NormalizeUrl(request.RequestEndpoint);
                    var baseString = CreateBaseString(url, requestParams, request.Parameters);
                    var hmacSha1 = new HMACSHA1 {Key = Encoding.ASCII.GetBytes(key)};
                    var hashedBytes = hmacSha1.ComputeHash(Encoding.ASCII.GetBytes(baseString));
                    signature = Convert.ToBase64String(hashedBytes);
                    break;
                default:
                    const string message = "OAuth signature method is not yet supported.";
                    throw new NotSupportedException(message);
            }

            return HttpEncoder.UrlEncode(signature);
        }

        private static IEnumerable<char> NormalizeUrl(Uri requestUri)
        {
            var scheme = requestUri.Scheme;
            var port = requestUri.Port;
            var host = requestUri.Host;

            var requestUrl = string.Format("{0}://{1}", scheme, host);
            var isDefaultPort = ((scheme == "http" && port == 80) || (requestUri.Scheme == "https" && requestUri.Port == 443));

            if (!isDefaultPort) {
                requestUrl += ":" + requestUri.Port;
            }

            requestUrl += requestUri.AbsolutePath;
            return requestUrl;
        }

        private static IEnumerable<char> NormalizeRequestParameters(IEnumerable<KeyValuePair<string, string>> @params)
        {
            using (var sw = new StringWriter()) {
                foreach (var pair in @params) {
                    sw.Write(pair.Key);
                    sw.Write("=");
                    sw.Write(pair.Value);
                    sw.Write("&");
                }

                sw.TrimEnd('&');
                return sw.ToString();
            }
        }

        private static string CreateBaseString(IEnumerable<char> url, IEnumerable<KeyValuePair<string, string>> requestParams, IEnumerable<KeyValuePair<string, string>> additional)
        {
            var combined = new SortedDictionary<string, string>();

            foreach (var p in requestParams) {
                combined.Add(p.Key, p.Value);
            }

            foreach (var p in additional) {
                combined.Add(p.Key, HttpEncoder.UrlEncode(p.Value));
            }

            var requestString = NormalizeRequestParameters(combined);

            using (var sw = new StringWriter()) {
                sw.Write("GET&");
                sw.WriteFormat("{0}&", HttpEncoder.UrlEncode(url));
                sw.WriteFormat("{0}", HttpEncoder.UrlEncode(requestString));
                return sw.ToString();
            }
        }
    }
}