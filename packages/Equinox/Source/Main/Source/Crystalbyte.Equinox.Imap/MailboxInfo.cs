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

using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Crystalbyte.Equinox.Imap.Text;
using Crystalbyte.Equinox.Text;

namespace Crystalbyte.Equinox.Imap
{
    [DebuggerDisplay("Name = {Name}, Exists = {Exists}")]
    public sealed class MailboxInfo
    {
        internal MailboxInfo(string mailboxName, MailboxPermissions permissions)
            : this(mailboxName)
        {
            Permissions = permissions;
        }

        internal MailboxInfo(string mailboxName)
        {
            Name = mailboxName;
        }

        /// <summary>
        ///   The full name of the mailbox.
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        ///   Read and/or write permission for the mailbox.
        /// </summary>
        public MailboxPermissions? Permissions { get; internal set; }

        /// <summary>
        ///   Defined flags in the mailbox.
        /// </summary>
        public MessageFlagGroup Flags { get; internal set; }

        /// <summary>
        ///   A list of message flags that the client can change
        ///   permanently.  If this is missing, the client should
        ///   assume that all flags can be changed permanently.
        /// </summary>
        public MessageFlagGroup PermanentFlags { get; internal set; }

        /// <summary>
        ///   A list of all message state changes within the associated mailbox.
        ///   This list will contain items, if i.E. a mail has received or lost a flag.
        /// </summary>
        public IEnumerable<MessageState> MessageStateChanges { get; internal set; }

        /// <summary>
        ///   The number of messages with the \Recent flag set.
        ///   See the description of the RECENT response for more detail.
        /// </summary>
        public int? Recent { get; internal set; }

        /// <summary>
        ///   The number of messages in the mailbox.  See the 
        ///   description of the EXISTS response for more detail.
        /// </summary>
        public int? Exists { get; internal set; }

        /// <summary>
        ///   The message sequence number of the first unseen
        ///   message in the mailbox.  If this is missing, the
        ///   client can not make any assumptions about the first
        ///   unseen message in the mailbox, and needs to issue a
        ///   SEARCH command if it wants to find it.
        /// </summary>
        public int? Unseen { get; internal set; }

        /// <summary>
        ///   The next unique identifier value.
        ///   If this is missing, the client can not make any assumptions about the
        ///   next unique identifier value.
        /// </summary>
        public int? UidNext { get; internal set; }

        /// <summary>
        ///   The unique identifier validity value.
        ///   If this is missing, the server does not support unique identifiers.
        /// </summary>
        public long? UidValidity { get; internal set; }


        /// <summary>
        ///   Identifies the corresponding status update item and adds it to the current instance.
        /// </summary>
        /// <param name = "line">The line to identify and parse.</param>
        internal void InjectLine(string line)
        {
            if (line.StartsWith("* OK")) {
                #region PermanentFlags

                {
                    var match = Regex.Match(line, RegexPatterns.ExaminePermanentFlagsPattern);
                    if (match.Success) {
                        var matches = Regex.Matches(match.Value.Substring(14), RegexPatterns.ExamineSingleFlagOrKeywordPattern);
                        if (PermanentFlags == null) {
                            PermanentFlags = new MessageFlagGroup(0x0000);
                        }
                        PermanentFlags.InjectMatches(matches);
                        return;
                    }
                }

                #endregion

                #region UidNext

                {
                    var match = Regex.Match(line, RegexPatterns.ExamineUidNextPattern);
                    if (match.Success) {
                        var digit = match.Value.Split(Characters.Space)[1];
                        UidNext = int.Parse(digit);
                        return;
                    }
                }

                #endregion

                #region Unseen

                {
                    var match = Regex.Match(line, RegexPatterns.ExamineUnseenPattern);
                    if (match.Success) {
                        var digit = match.Value.Split(Characters.Space)[1];
                        Unseen = int.Parse(digit);
                        return;
                    }
                }

                #endregion

                #region UidValidity

                {
                    var match = Regex.Match(line, RegexPatterns.ExamineUidValidityPattern);
                    if (match.Success) {
                        var digit = match.Value.Split(Characters.Space)[1];
                        UidValidity = long.Parse(digit);
                        return;
                    }
                }

                #endregion

                return;
            }

            #region Flags

            {
                var match = Regex.Match(line, RegexPatterns.ExamineAndFetchFlagsPattern);
                if (match.Success) {
                    var matches = Regex.Matches(match.Value.Substring(5), RegexPatterns.ExamineSingleFlagOrKeywordPattern);
                    if (Flags == null) {
                        Flags = new MessageFlagGroup(0x0000);
                    }
                    Flags.InjectMatches(matches);
                    return;
                }
            }

            #endregion

            #region Exists

            if (line.Contains("EXISTS")) {
                var value = Regex.Match(line, @"\d+").Value;
                Exists = int.Parse(value);
                return;
            }

            #endregion

            #region Recent

            if (line.Contains("RECENT")) {
                var value = Regex.Match(line, @"\d+").Value;
                Recent = int.Parse(value);
                return;
            }

            #endregion
        }
    }
}