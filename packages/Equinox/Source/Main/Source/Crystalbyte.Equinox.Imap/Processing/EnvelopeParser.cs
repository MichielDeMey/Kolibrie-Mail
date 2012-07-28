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
using System.Text.RegularExpressions;
using Crystalbyte.Equinox.Imap.Text;
using Crystalbyte.Equinox.Mime.Text;

namespace Crystalbyte.Equinox.Imap.Processing
{
    internal sealed class EnvelopeParser : ISectionParser
    {
        #region ISectionParser Members

        public object Parse(string text, object argument)
        {
            var envelope = new Envelope();

            // quotes inside the subject can trash the regex results, therefor we will tempararily replace them
            text = text.Replace("\\\"", "&,,4+");

            var matches = Regex.Matches(text, RegexPatterns.EnvelopeResponsePattern);
            if (matches.Count != 10) {
                Debug.WriteLine(FailureMessages.UnexpectedItemCountInEnvelopeMessage);
                Debug.WriteLine("Response: " + text);
                return envelope;
            }

            DateTime date;
            var normalized = TransferEncoder.DecodeHeaderIfNecessary(matches[0].Value).TrimQuotes().RemoveComments();
            var success = DateTime.TryParse(normalized, out date);
            if (success) {
                envelope.Date = date;
            } else {
                Debug.WriteLine(FailureMessages.UnexpectedDateFormatMessage);
                Debug.WriteLine("Date: " + normalized);
            }

            envelope.Subject = matches[1].Value.TrimQuotes();

            // need to insert the previously replaced quotes
            envelope.Subject = envelope.Subject.Replace("&,,4+", "\"");
            envelope.Subject = TransferEncoder.DecodeHeaderIfNecessary(envelope.Subject);

            {
                var value = TransferEncoder.DecodeHeaderIfNecessary(matches[2].Value);
                var from = ParseContacts(value.TrimQuotes());
                ((List<EmailContact>) envelope.From).AddRange(from);
            }

            {
                var value = TransferEncoder.DecodeHeaderIfNecessary(matches[3].Value);
                var sender = ParseContacts(value.TrimQuotes());
                ((List<EmailContact>) envelope.Sender).AddRange(sender);
            }

            {
                var value = TransferEncoder.DecodeHeaderIfNecessary(matches[4].Value);
                var replyTo = ParseContacts(value.TrimQuotes());
                ((List<EmailContact>) envelope.ReplyTo).AddRange(replyTo);
            }

            {
                var value = TransferEncoder.DecodeHeaderIfNecessary(matches[5].Value);
                var to = ParseContacts(value.TrimQuotes());
                ((List<EmailContact>) envelope.To).AddRange(to);
            }

            {
                var value = TransferEncoder.DecodeHeaderIfNecessary(matches[6].Value);
                var cc = ParseContacts(value.TrimQuotes());
                ((List<EmailContact>) envelope.Cc).AddRange(cc);
            }

            {
                var value = TransferEncoder.DecodeHeaderIfNecessary(matches[7].Value);
                var bcc = ParseContacts(value.TrimQuotes());
                ((List<EmailContact>) envelope.Bcc).AddRange(bcc);
            }

            var inReplyTo = TransferEncoder.DecodeHeaderIfNecessary(matches[8].Value).TrimQuotes();
            envelope.InReplyTo = inReplyTo.IsNilOrEmpty() ? string.Empty : inReplyTo;

            var messageId = TransferEncoder.DecodeHeaderIfNecessary(matches[9].Value).TrimQuotes();
            envelope.MessageId = messageId.IsNilOrEmpty() ? string.Empty : messageId;

            return envelope;
        }

        #endregion

        private static IEnumerable<EmailContact> ParseContacts(string value)
        {
            if (string.IsNullOrEmpty(value) || value == "NIL") {
                yield break;
            }

            var matches = Regex.Matches(value, RegexPatterns.QuotedItemsOrNilPattern);
            for (var i = 0; i < matches.Count; i += 4) {
                var name = !matches[i].Value.IsNilOrEmpty() ? matches[i].Value.TrimQuotes() : string.Empty;
                var email = matches[i + 2].Value.TrimQuotes() + "@" + matches[i + 3].Value.TrimQuotes();

                yield return new EmailContact(name, email);
            }
        }
    }
}