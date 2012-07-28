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
using System.Linq;
using Crystalbyte.Equinox.Imap.Processing;
using Crystalbyte.Equinox.Imap.Processing.Advanced;
using Crystalbyte.Equinox.Mime.Collections;

namespace Crystalbyte.Equinox.Imap
{
    public sealed class MessageInfo : IFetchable
    {
        private MessageInfo()
        {
            NestedMessages = new List<MessageInfo>();
            Attachments = new List<AttachmentInfo>();
            Views = new List<ViewInfo>();
        }

        public Envelope Envelope { get; private set; }
        public IEnumerable<ViewInfo> Views { get; set; }
        public IEnumerable<AttachmentInfo> Attachments { get; private set; }
        public IEnumerable<MessageInfo> NestedMessages { get; private set; }
        internal string OriginalString { get; set; }

        #region IFetchable Members

        public string Token { get; internal set; }
        public int SequenceNumber { get; internal set; }

        #endregion

        internal static MessageInfo FromBodyPart(Part part, int sn, string original)
        {
            var rootToken = string.Empty;
            var message = new MessageInfo {SequenceNumber = sn, Token = rootToken, OriginalString = original};
            if (part is BodyPart) {
                var body = (BodyPart) part;
                switch (body.BodyType.ToLower()) {
                    case "text":
                        AddView(message, part as BodyPart, rootToken, sn);
                        break;
                    case "audio":
                    case "video":
                    case "image":
                    case "application":
                        AddAttachment(message, part as BodyPart, rootToken, sn);
                        break;
                    case "message":
                        if (body.SubType == "rfc822") {
                            AddNestedMessage(message, part as BodyPart, rootToken, sn, original);
                        } else {
                            AddView(message, part as BodyPart, rootToken, sn);
                        }
                        break;
                    default:
                        AddView(message, part as BodyPart, string.Empty, sn);
                        break;
                }
            }

            var i = 0;
            foreach (var child in part.Children) {
                i += 1;
                ProcessChild(message, child, i.ToString(), sn, original);
            }

            return message;
        }

        private static void ProcessChild(MessageInfo message, Part current, string token, int sn, string original)
        {
            if (current is BodyPart) {
                var body = (BodyPart) current;
                switch (body.BodyType.ToLower()) {
                    case "text":
                        AddView(message, current as BodyPart, token, sn);
                        break;
                    case "audio":
                    case "video":
                    case "image":
                    case "application":
                        AddAttachment(message, current as BodyPart, token, sn);
                        break;
                    case "message":
                        if (body.SubType.ToLower() == "rfc822") {
                            AddNestedMessage(message, body, token, sn, original);
                            return;
                        }
                        AddView(message, current as BodyPart, token, sn);
                        break;
                    default:
                        AddView(message, current as BodyPart, token, sn);
                        break;
                }
            }

            var i = 0;
            foreach (var child in current.Children) {
                i += 1;
                var childToken = (token + "." + i).TrimStart('.');
                ProcessChild(message, child, childToken, sn, original);
            }
        }

        private static void AddNestedMessage(MessageInfo message, BodyPart current, string token, int sn, string original)
        {
            var nested = new MessageInfo { SequenceNumber = sn, Token = token, OriginalString = original }; 
            nested.Token = token;
            nested.SequenceNumber = sn;

            var envelope = original.Substring(current.EnvelopeBounds[0], current.EnvelopeBounds[1]);
            var parser = new EnvelopeParser();
            nested.Envelope = (Envelope) parser.Parse(envelope, null);
            ((List<MessageInfo>) message.NestedMessages).Add(nested);

            var i = 0;
            foreach (var child in current.Children) {
                i += 1;
                var childToken = (token + "." + i).TrimStart('.');
                ProcessChild(nested, child, childToken, sn, original);
            }
        }

        private static void AddAttachment(MessageInfo message, BodyPart child, string token, int sn)
        {
            var attachment = new AttachmentInfo {
                ContentTransferEncoding = string.IsNullOrEmpty(child.BodyEncoding) ? string.Empty : child.BodyEncoding.ToLower(), 
                MediaType = (child.BodyType + "/" + child.SubType).ToLower(), 
                Token = token, 
                SequenceNumber = sn
            };

            int size;
            var success = int.TryParse(child.BodySize, out size);
            if (success) {
                attachment.SizeEncoded = Size.FromBytes(size);
            } else {
                Debug.WriteLine(string.Format("Unable to parse body size. Value is not numeric: '{0}'", child.BodySize));
            }

            if (child.Parameters.ContainsKey("name")) {
                attachment.Name = child.Parameters["name"];
            }

            ((ParameterDictionary) attachment.Parameters).AddRange(child.Parameters);
            ((IList<AttachmentInfo>) message.Attachments).Add(attachment);
        }

        private static void AddView(MessageInfo message, BodyPart child, string token, int sn)
        {
            var mediaType = (child.BodyType + "/" + child.SubType).ToLower();
            var view = new ViewInfo {
                Description = child.BodyDescription.ToUpper() == "NIL" ? string.Empty : child.BodyDescription, Id = child.BodyId.ToUpper() == "NIL" ? string.Empty : child.BodyId, 
                ContentTransferEncoding = string.IsNullOrEmpty(child.BodyEncoding) ? string.Empty : child.BodyEncoding.ToLower(), 
                MediaType = mediaType, 
                Token = token, 
                SequenceNumber = sn
            };

            int size;
            var success = int.TryParse(child.BodySize, out size);
            if (success) {
                view.SizeEncoded = Size.FromBytes(size);
            } else {
                Debug.WriteLine(string.Format("Unable to parse body size. Value is not numeric: '{0}'", child.BodySize));
            }

            int lines;
            success = int.TryParse(child.TextLines, out lines);
            if (success) {
                view.TextLinesEncoded = lines;
            }

            var paramDictionary = (ParameterDictionary)view.Parameters;
            paramDictionary.AddRange(child.Parameters);
            ((IList<ViewInfo>) message.Views).Add(view);
        }
    }
}