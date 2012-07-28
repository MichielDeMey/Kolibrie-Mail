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
using System.Linq;
using System.Text;
using Crystalbyte.Equinox.Processing;

namespace Crystalbyte.Equinox.Mime
{
    public static class EntityExtensions
    {
        public static bool IsMessage(this Entity entity)
        {
            if (!entity.HasContentType) {
                return false;
            }

            return entity.ContentTypeHeaderField.MediaType == ContentTypes.MessageRfc822;
        }

        public static bool IsRelated(this Entity entity)
        {
            return entity.Headers.Any(x => x.Name.ToLower() == FieldNames.ContentId.ToLower());
        }

        public static bool IsAttachment(this Entity entity)
        {
            if (!entity.HasContentDisposition) {
                return false;
            }
            return entity.ContentDispositionHeaderField.DispositionType == DispositionTypes.Attachment;
        }

        public static bool IsView(this Entity entity)
        {
            return entity.HasContentType && entity.ContentTypeHeaderField.MediaType.TrimQuotes().Trim().StartsWith("text");
        }

        public static Attachment ToRelated(this Entity entity)
        {
            var contentId = entity.Headers.Where(x => x.Name.ToLower() == FieldNames.ContentId.ToLower()).First().Value;
            var bytes = !entity.IsBinary ? Encoding.UTF8.GetBytes(entity.Text) : entity.Bytes;
            var attachment = Attachment.FromBytes(string.Empty, bytes, entity.ContentTypeHeaderField.MediaType);
            attachment.ContentId = contentId;
            return attachment;
        }

        public static Attachment ToAttachment(this Entity entity)
        {
            var filename = string.Empty;
            if (entity.ContentTypeHeaderField.Parameters.ContainsKey("name")) {
                filename = entity.ContentTypeHeaderField.Parameters["name"];
            }

            var bytes = !entity.IsBinary ? Encoding.UTF8.GetBytes(entity.Text) : entity.Bytes;
            var attachment = Attachment.FromBytes(filename, bytes, entity.ContentTypeHeaderField.MediaType);
            return attachment;
        }

        public static View ToView(this Entity entity)
        {
            var mediaType = entity.HasContentType ? entity.ContentTypeHeaderField.MediaType : MediaTypes.TextPlain;
            return new View {MediaType = mediaType, Text = entity.Text};
        }

        public static Entity FromView(View view)
        {
            var entity = Entity.CreateUtf8(view.MediaType, ContentTransferEncodings.Base64, false);
            entity.Text = view.Text;
            return entity;
        }

        public static Entity FromMessage(Message message)
        {
            Entity entity;
            var isMixed = false;
            var isAlternative = false;

            if (message.Attachments.Count > 0 || message.NestedMessages.Count > 0) {
                isMixed = true;
            }

            if (message.Views.Count > 1) {
                isAlternative = true;
            }

            // create dummy view without content if none was provided
            if (message.Views.Count == 0) {
                var dummy = new View {Text = string.Empty, MediaType = MediaTypes.TextPlain};
                message.Views.Add(dummy);
            }

            if (isMixed) {
                entity = Entity.CreateMultipartMixed();

                if (isAlternative) {
                    var alternative = CreateAlternative(message.Views);
                    entity.Children.Add(alternative);
                } else {
                    var view = message.Views.First();
                    entity.Children.Add(view.IsRelated ? FromRelatedView(view) : FromView(view));
                }

                foreach (var nestedMessage in message.NestedMessages) {
                    // create message container entity
                    var nested = Entity.Create(ContentTypes.MessageRfc822, ContentTransferEncodings.Base64);
                    // create, serialize the nested message and insert in container as content
                    nested.Text = FromMessage(nestedMessage).Serialize();
                    entity.Children.Add(nested);
                }

                // this is curious syntax
                foreach (var ent in message.Attachments.Select(FromAttachment)) {
                    entity.Children.Add(ent);
                }
            } else {
                if (isAlternative) {
                    entity = CreateAlternative(message.Views);
                } else {
                    var view = message.Views.First();
                    entity = view.IsRelated ? FromRelatedView(view) : FromView(view);
                }
            }

            var subject = message.Subject ?? string.Empty;
            entity.Headers.Add(new HeaderField("Subject", subject));

            var to = message.To.ToHeaderString();
            entity.Headers.Add(new HeaderField("To", to));

            var from = message.From.ToHeaderString();
            entity.Headers.Add(new HeaderField("From", from));

            var cc = message.Ccs.ToHeaderString();
            if (!string.IsNullOrEmpty(cc)) {
                entity.Headers.Add(new HeaderField("Cc", cc));
            }

            var date = message.Date.ToMimeDateTimeString();
            if (!string.IsNullOrEmpty(date)) {
                entity.Headers.Add(new HeaderField("Date", date));
            }

            var bcc = message.Bccs.ToHeaderString();
            if (!string.IsNullOrEmpty(bcc)) {
                entity.Headers.Add(new HeaderField("Bcc", bcc));
            }

            var replyTo = message.ReplyTo ?? string.Empty;
            if (!string.IsNullOrEmpty(replyTo)) {
                entity.Headers.Add(new HeaderField("Reply-To", replyTo));
            }

            foreach (var header in from header in message.Headers let local = header let contains = entity.Headers.Any(x => x.Name.ToLower() == local.Name.ToLower()) where !contains select header) {
                entity.Headers.Add(header);
            }

            return entity;
        }

        private static Entity FromRelatedView(View view)
        {
            var entity = Entity.CreateMultipartRelated();
            entity.Children.Add(FromView(view));
            foreach (var attachment in view.RelatedAttachments) {
                entity.Children.Add(FromRelatedAttachment(attachment));
            }
            return entity;
        }

        private static Entity FromRelatedAttachment(Attachment attachment)
        {
            return FromAttachmentInternal(attachment, false);
        }

        private static Entity FromAttachmentInternal(Attachment attachment, bool hasBoundary)
        {
            var entity = Entity.Create(attachment.MediaType, ContentTransferEncodings.Base64, hasBoundary);
            entity.Bytes = attachment.Bytes;
            entity.ContentTypeHeaderField.ContentName = attachment.Filename;

            entity.Headers.Add(new ContentDispositionHeaderField());
            entity.ContentDispositionHeaderField.Filename = attachment.Filename;

            if (string.IsNullOrEmpty(attachment.ContentId)) {
                entity.ContentDispositionHeaderField.DispositionType = DispositionTypes.Attachment;
            } else {
                entity.Headers.Add(new HeaderField {Name = FieldNames.ContentId, Value = attachment.ContentId});
                entity.ContentDispositionHeaderField.DispositionType = DispositionTypes.Inline;
            }
            return entity;
        }

        private static Entity FromAttachment(Attachment attachment)
        {
            return FromAttachmentInternal(attachment, true);
        }

        private static Entity CreateAlternative(IEnumerable<View> views)
        {
            var entity = Entity.CreateMultipartAlternative();
            foreach (var view in views) {
                entity.Children.Add(view.IsRelated ? FromRelatedView(view) : FromView(view));
            }
            return entity;
        }

        public static Message ToMessage(this Entity entity)
        {
            var message = new Message();
            var subjectHeader = entity.Headers.Where(x => x.Name.ToLower() == "subject").FirstOrDefault();
            if (subjectHeader != null) {
                message.Subject = subjectHeader.Value;
            }

            var dateHeader = entity.Headers.Where(x => x.Name.ToLower() == "date").FirstOrDefault();
            if (dateHeader != null) {
                message.Date = DateTime.Parse(dateHeader.Value.RemoveComments());
            }

            var contactParser = new ContactCollectionParser();
            var fromHeader = entity.Headers.Where(x => x.Name.ToLower() == "from").FirstOrDefault();
            if (fromHeader != null) {
                var contacts = (IEnumerable<EmailContact>) contactParser.Parse(fromHeader.Value);
                ((List<EmailContact>) message.From).AddRange(contacts);
            }

            var toHeader = entity.Headers.Where(x => x.Name.ToLower() == "to").FirstOrDefault();
            if (toHeader != null) {
                var contacts = (IEnumerable<EmailContact>) contactParser.Parse(toHeader.Value);
                ((List<EmailContact>) message.To).AddRange(contacts);
            }

            var ccHeader = entity.Headers.Where(x => x.Name.ToLower() == "cc").FirstOrDefault();
            if (ccHeader != null) {
                var contacts = (IEnumerable<EmailContact>) contactParser.Parse(ccHeader.Value);
                ((List<EmailContact>) message.Ccs).AddRange(contacts);
            }

            var bccHeader = entity.Headers.Where(x => x.Name.ToLower() == "bcc").FirstOrDefault();
            if (bccHeader != null) {
                var contacts = (IEnumerable<EmailContact>) contactParser.Parse(bccHeader.Value);
                ((List<EmailContact>) message.Bccs).AddRange(contacts);
            }

            var replyToHeader = entity.Headers.Where(x => x.Name.ToLower() == "reply-to").FirstOrDefault();
            if (replyToHeader != null) {
                message.ReplyTo = replyToHeader.Value;
            }

            message.Headers.AddRange(entity.Headers);
            ParseEntity(message, entity, null);

            return message;
        }

        private static View ParseEntity(Message message, Entity current, Entity last, View related = null)
        {
            if (current.IsMessage()) {
                var e = new Entity();
                e.Deserialize(current.Text);
                var nested = e.ToMessage();
                message.NestedMessages.Add(nested);
                return null;
            }

            if (related != null) {
                var attachment = current.ToRelated();
                related.RelatedAttachments.Add(attachment);
                return related;
            }

            if (current.IsAttachment()) {
                var attachment = current.ToAttachment();
                message.Attachments.Add(attachment);
                return null;
            }

            if (current.IsView() || !current.HasContentType) {
                var view = current.ToView();
                message.Views.Add(view);
                if (last != null && last.IsMultipartRelated) {
                    return view;
                }
                return null;
            }

            foreach (var child in current.Children) {
                // we need to pass the related view to the next sibling
                related = ParseEntity(message, child, current, related);
            }

            return null;
        }
    }
}