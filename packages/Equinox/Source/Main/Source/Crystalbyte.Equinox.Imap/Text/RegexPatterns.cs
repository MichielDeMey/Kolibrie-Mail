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

namespace Crystalbyte.Equinox.Imap.Text
{
    internal static class RegexPatterns
    {
        public const string SequenceIdPattern = @"^[aA]\d+ (NO|OK|BAD)";
        public const string ReadWritePattern = @"\[READ-WRITE\]";
        public const string ReadOnlyPattern = @"\[READ-ONLY\]";
        public const string ExamineAndFetchFlagsPattern = @"FLAGS \(.*?\)";
        public const string ExamineSingleFlagOrKeywordPattern = @"([a-zA-Z]+)|(\*)";
        public const string ExamineExistsPattern = @"\d+ EXISTS";
        public const string ExamineRecentPattern = @"\d+ RECENT";
        public const string ExaminePermanentFlagsPattern = @"PERMANENTFLAGS \(.*\)";
        public const string ExamineUidNextPattern = @"UIDNEXT \d+";
        public const string ExamineUnseenPattern = @"UNSEEN \(.*\)";
        public const string ExamineUidValidityPattern = @"UIDVALIDITY \d+";
        public const string NonBase64CharactersPattern = @"[\u007F-\uFFFF\u0000-\u001F]+|&";
        public const string Rfc2060ModifiedBase64Pattern = "&.*?-";
        public const string HeaderInlineCommentPattern = @"\(.+\)";
        public const string BodyStructureResponsePattern = "BODYSTRUCTURE";
        public const string InternalDateResponsePattern = "INTERNALDATE \\\".+\\\"";
        public const string QuotedItemsOrNilPattern = "\".*?\"|NIL";
        public const string QuotedItemsPattern = "\".*?\"";
        public const string EnvelopeResponsePattern = "\\(\\(.+?\\)\\)|NIL|\"\"|<.+?>|\".+?\"";
        public const string ParenthesizedItemPattern = @"\(.+?\)";
        public const string CurlyParanthesizedSizePattern = "\\{\\d+\\}";
        public const string BoundaryEnvelope = @"^[\r\n\s]*-+[^\r\n]+|\s*-+[^\r\n]+--(\r\n)*$";
        public const string SingleFlagPattern = @"\\\w+";
        public const string EmailBracketPattern = "<.+>";
        public const string BodyPartCommandPattern = @"BODY[((\d+\.)+)(TEXT|HEADER|\.)]";
    }
}