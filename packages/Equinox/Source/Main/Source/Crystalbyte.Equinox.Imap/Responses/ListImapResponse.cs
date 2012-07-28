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
using System.Text.RegularExpressions;
using Crystalbyte.Equinox.Imap.Text;
using Crystalbyte.Equinox.Text;

namespace Crystalbyte.Equinox.Imap.Responses
{
    public sealed class ListImapResponse : ImapResponse
    {
        private readonly List<Mailbox> _mailboxes;

        public ListImapResponse()
        {
            _mailboxes = new List<Mailbox>();
        }

        public IEnumerable<Mailbox> Mailboxes
        {
            get { return _mailboxes; }
        }

        protected internal override void Parse(ImapResponseReader reader)
        {
            while (true) {
                if (reader.IsCompleted) {
                    TakeSnapshot(reader);
                    break;
                }

                if (!reader.IsUntagged) {
                    continue;
                }

                var mailbox = new Mailbox();
                var line = reader.CurrentLine;
                // pattern is identical to message flags
                var attMatches = Regex.Matches(line, RegexPatterns.SingleFlagPattern);
                foreach (Match match in attMatches) {
                    NameAttributes flag;
                    var value = match.Value.TrimAnySeperate(1, 0);
                    var success = Enum.TryParse(value, true, out flag);
                    if (success) {
                        mailbox.Attributes |= flag;
                    } else {
                        ((IList<string>) mailbox.Keywords).Add(match.Value);
                    }
                }
                string encodedName;

                var quotedItemsMatch = Regex.Matches(line, RegexPatterns.QuotedItemsPattern);
                if (quotedItemsMatch.Count == 1) {
                    mailbox.Delimiter = quotedItemsMatch[0].Value.TrimQuotes()[0];
                }
                if (quotedItemsMatch.Count == 2) {
                    mailbox.Delimiter = quotedItemsMatch[0].Value.TrimQuotes()[0];
                    encodedName = quotedItemsMatch[1].Value.TrimQuotes();
                } else {
                    var split = line.Split(Characters.Space);
                    var lastIndex = split.Length - 1;
                    encodedName = split[lastIndex].TrimQuotes();
                }

                // we need to decode ASCII names according to IMAP specs.
                // http://tools.ietf.org/html/rfc2060#section-5.1.3
                mailbox.Name = MailboxNameEncoder.Decode(encodedName);

                _mailboxes.Add(mailbox);

                reader.ReadNextLine();
            }
        }
    }
}