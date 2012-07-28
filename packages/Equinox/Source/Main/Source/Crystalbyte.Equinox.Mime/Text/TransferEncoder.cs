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
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Crystalbyte.Equinox.Mime.Text
{
    public static class TransferEncoder
    {
        public static string Encode(string text, string transferEncoding, string charset, bool useBockText = true)
        {
            switch (transferEncoding) {
                case ContentTransferEncodings.QuotedPrintable:
                    {
                        var bytes = Encoding.UTF8.GetBytes(text);
                        return QuotedPrintableConverter.ToQuotedPrintableString(bytes);
                    }
                case ContentTransferEncodings.None:
                    {
                        return text;
                    }
                default:
                    {
                        var bytes = Encoding.UTF8.GetBytes(text);
                        var encodedText = Convert.ToBase64String(bytes);
                        return useBockText ? encodedText.ToBlockText(76) : encodedText;
                    }
            }
        }

        public static string Decode(string literals, string transferEncoding, string charset)
        {
            Encoding targetEncoding;

            // Cp1252 is not recognized under this name
            if (charset.ToLower() == "cp1252") {
                charset = Charsets.Ansi;
            }

            try {
                // if this goes haywire
                targetEncoding = Encoding.GetEncoding(charset);
            }
            catch (Exception) {
                // try this one
                targetEncoding = Encoding.UTF8;
            }

            switch (transferEncoding.ToLower()) {
                case ContentTransferEncodings.QuotedPrintable:
                    {
                        return QuotedPrintableConverter.FromQuotedPrintable(literals, targetEncoding);
                    }
                case ContentTransferEncodings.Base64:
                    {
                        var bytes = Convert.FromBase64String(literals);
                        return targetEncoding.GetString(bytes);
                    }
                default:
                    {
                        // no encoding
                        return literals;
                    }
            }
        }

        public static string DecodeHeaderIfNecessary(string text)
        {
            text = Regex.Replace(text, RegexPatterns.EncodedHeaderFieldPattern, match => DecodeHeaderBlock(match.Value));

            // change _ to 'space'
            // http://www.faqs.org/rfcs/rfc1342.html
            text = text.Replace(Characters.Underscore, Characters.Space);
            return text;
        }

        private static string DecodeHeaderBlock(string text)
        {
            text = text.Trim(Characters.EqualitySign).Trim(Characters.QuestionMark);
            var split = text.Split(Characters.QuestionMark);
            Debug.Assert(split.Length == 3);

            var charset = split[0];
            var encoding = split[1].ToUpper();
            var message = split[2];

            string decodedText;

            switch (encoding) {
                case "Q":
                    {
                        decodedText = Decode(message, ContentTransferEncodings.QuotedPrintable, charset);
                        break;
                    }
                default:
                    {
                        decodedText = Decode(message, ContentTransferEncodings.Base64, charset);
                        break;
                    }
            }

            return decodedText;
        }

        public static string EncodeHeaderIfNecessary(string text, HeaderEncodingTypes headerEncoding)
        {
            var chars = text.ToCharArray();
            var needsEncoding = chars.Any(character => character > 128);

            if (!needsEncoding) {
                return text;
            }

            switch (headerEncoding) {
                case HeaderEncodingTypes.Base64:
                    return EncodeHeaderBase64(chars);
                default:
                    return EncodeHeaderQuotedPrintable(chars);
            }
        }

        private static string EncodeHeaderQuotedPrintable(char[] chars)
        {
            var text = new string(chars);
            var message = Encode(text, ContentTransferEncodings.QuotedPrintable, Charsets.Utf8);
            return string.Format("=?{0}?Q?{1}?=", Charsets.Utf8, message);
        }

        private static string EncodeHeaderBase64(char[] chars)
        {
            var text = new string(chars);
            var message = Encode(text, ContentTransferEncodings.Base64, Charsets.Utf8, false);
            return string.Format("=?{0}?B?{1}?=", Charsets.Utf8, message);
        }
    }
}