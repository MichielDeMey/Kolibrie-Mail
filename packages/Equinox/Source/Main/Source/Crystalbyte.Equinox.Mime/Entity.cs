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
using System.IO;
using System.Linq;
using Crystalbyte.Equinox.Mime.Text;

namespace Crystalbyte.Equinox.Mime
{
    /// <summary>
    ///   The term "entity", refers specifically to the MIME-defined header
    ///   fields and contents of either a message or one of the parts in the
    ///   body of a multipart entity.  The specification of such entities is
    ///   the essence of MIME.
    ///   http://tools.ietf.org/html/rfc2045#section-2.4
    /// </summary>
    [DebuggerTypeProxy(typeof (EntityDebuggerProxy))]
    public sealed class Entity
    {
        public Entity()
        {
            DefaultTransferEncoding = ContentTransferEncodings.None;
            DefaultContentType = ContentTypes.TextPlain;
            DefaultCharset = Charsets.Utf8;

            Headers = new List<HeaderField>();
            Children = new List<Entity>();
        }

        public bool IsMultipartRelated
        {
            get { return HasContentType && string.Compare(ContentTypeHeaderField.MediaType, ContentTypes.MultipartRelated, true) == 0; }
        }

        public ContentTransferEncodingHeaderField ContentTransferEncodingHeaderField
        {
            get
            {
                ContentTransferEncodingHeaderField field;
                TryGetHeaderField(FieldNames.ContentTransferEncoding, out field);
                return field;
            }
        }

        public ContentTypeHeaderField ContentTypeHeaderField
        {
            get
            {
                ContentTypeHeaderField field;
                TryGetHeaderField(FieldNames.ContentType, out field);
                return field;
            }
        }

        public ContentDispositionHeaderField ContentDispositionHeaderField
        {
            get
            {
                ContentDispositionHeaderField field;
                TryGetHeaderField(FieldNames.ContentDisposition, out field);
                return field;
            }
        }

        public bool HasContentDisposition
        {
            get { return ContentDispositionHeaderField != null; }
        }

        public bool HasContentType
        {
            get { return ContentTypeHeaderField != null; }
        }

        public bool HasContentTransferEncoding
        {
            get { return ContentTransferEncodingHeaderField != null; }
        }

        public IList<HeaderField> Headers { get; private set; }

        public IList<Entity> Children { get; private set; }

        internal bool IsBoundaryExpected
        {
            get { return ContentTypeHeaderField != null && ContentTypeHeaderField.HasBoundary; }
        }

        public string Text { get; set; }
        public byte[] Bytes { get; set; }

        private string DefaultContentType { get; set; }
        private string DefaultTransferEncoding { get; set; }
        private string DefaultCharset { get; set; }

        public bool IsBinary
        {
            get { return Bytes != null; }
        }

        public string Serialize()
        {
            using (var writer = new StringWriter()) {
                foreach (var literals in Headers.Select(headerField => headerField.Serialize())) {
                    writer.WriteLine(literals);
                }

                var isSingleView = CheckSingleView();
                if (isSingleView) {
                    var child = Children.First();
                    writer.WriteLine(string.Empty);
                    writer.WriteLine(child.Text);
                    return writer.ToString();
                }

                var isMultipart = CheckMultipart();
                if (isMultipart) {
                    writer.WriteLine(string.Empty);
                    writer.WriteLine("This is a multi-part message in MIME format.");

                    foreach (var child in Children) {
                        writer.WriteLine(string.Empty);
                        writer.WriteLine("--" + ContentTypeHeaderField.BoundaryName);
                        var part = child.Serialize();
                        writer.WriteLine(part);
                    }

                    writer.WriteLine("--" + ContentTypeHeaderField.BoundaryName + "--");
                } else {
                    var encoding = DetermineEncoding();
                    var contentType = DetermineContentType();
                    switch (contentType) {
                        case ContentTypes.MessageRfc822:
                            encoding = ContentTransferEncodings.None;
                            break;
                    }
                    var charset = DetermineCharset();

                    var encodedBody = !string.IsNullOrEmpty(Text)
                                          ? TransferEncoder.Encode(Text, encoding, charset) // is text, needs encoding
                                          : Bytes != null ? Convert.ToBase64String(Bytes).ToBlockText(76) : string.Empty; // is binary stream, needs no encoding

                    writer.Write(Environment.NewLine);
                    writer.Write(encodedBody);
                }

                return writer.ToString();
            }
        }

        public void Deserialize(string literals)
        {
            var isBeginning = true;
            LineInfo? last = null;
            using (var reader = new StringReader(literals)) {
                while (true) {
                    var line = reader.ReadLine();
                    if (line == null) {
                        break;
                    }

                    // darn new lines
                    if (line == string.Empty && isBeginning) {
                        continue;
                    }

                    isBeginning = false;
                    var current = new LineInfo(line);
                    if (current.IsField) {
                        if (last.HasValue) {
                            CommitHeaderField(last.Value);
                        }

                        last = current;
                        continue;
                    }

                    if (current.IsContinuation) {
                        if (last.HasValue) {
                            last = last.Value.Merge(current);
                        }
                        continue;
                    }

                    if (!current.IsBoundaryStart && IsBoundaryExpected) {
                        continue;
                    }

                    // process final field
                    if (last.HasValue) {
                        CommitHeaderField(last.Value);
                        last = null;
                    }

                    if (!IsBoundaryExpected) {
                        var charset = DetermineCharset();
                        var encoding = DetermineEncoding();
                        var content = reader.ReadToEnd() ?? string.Empty;
                        content.Trim();

                        if (!string.IsNullOrEmpty(content)) {
                            if (HasContentType) {
                                var contentType = ContentTypeHeaderField.MediaType;
                                if (encoding == ContentTransferEncodings.Base64 && !contentType.StartsWith("text")) {
                                    Bytes = string.IsNullOrEmpty(content) ? new byte[0] : Convert.FromBase64String(content);
                                } else {
                                    Text = TransferEncoder.Decode(content, encoding, charset);
                                }
                            } else {
                                Text = TransferEncoder.Decode(content, encoding, charset);
                            }
                        }
                        break;
                    }

                    if (current.IsBoundaryStart && IsExpectedBoundary(current)) {
                        var parts = ReadBoundaryBlocks(current.BoundaryName, reader);
                        foreach (var part in parts) {
                            var entity = new Entity();
                            entity.Deserialize(part);
                            Children.Add(entity);
                        }
                    }
                }
            }
        }

        private bool IsExpectedBoundary(LineInfo line) {
            return line.Text.ToLower() == ContentTypeHeaderField.BoundaryName.ToLower();
        }

        private string DetermineCharset()
        {
            if (HasContentType) {
                var header = ContentTypeHeaderField;
                if (string.IsNullOrEmpty(header.Charset)) {
                    return DefaultCharset;
                }
                return header.Charset.Trim(Characters.Quote);
            }
            return DefaultCharset;
        }

        internal string DetermineContentType()
        {
            if (HasContentType) {
                var header = ContentTypeHeaderField;
                if (string.IsNullOrEmpty(header.MediaType)) {
                    return DefaultContentType;
                }
                return header.MediaType;
            }
            return DefaultContentType;
        }

        private string DetermineEncoding()
        {
            if (HasContentTransferEncoding) {
                var header = ContentTransferEncodingHeaderField;
                if (!string.IsNullOrEmpty(header.Encoding)) {
                    return ContentTransferEncodingHeaderField.Encoding;
                }
                return DefaultTransferEncoding;
            }
            return DefaultTransferEncoding;
        }

        private bool CheckSingleView()
        {
            if (Children.Count != 1) {
                return false;
            }

            var entity = Children.First();
            if (!entity.HasContentType) {
                return false;
            }

            return entity.ContentTypeHeaderField.MediaType.StartsWith("text");
        }

        private bool CheckMultipart()
        {
            if (Children.Count < 1) {
                return false;
            }

            var firstContentType = Children.First().DetermineContentType();
            var identical = Children.Select(x => x.DetermineContentType() == firstContentType);
            return identical.Count() == Children.Count;
        }

        public bool TryGetHeaderField<T>(string name, out T field) where T : HeaderField
        {
            if (Headers.Any(x => x.Name.ToLower() == name.ToLower())) {
                field = GetHeaderField<T>(name);
                return true;
            }
            field = null;
            return false;
        }

        public T GetHeaderField<T>(string name) where T : HeaderField
        {
            return Headers.Where(x => x.Name.ToLower() == name.ToLower()).First() as T;
        }

        private static IEnumerable<string> ReadBoundaryBlocks(string name, TextReader reader)
        {
            var parts = new List<string>();
            var boundaryStart = "--" + name;
            var boundaryTerminator = "--" + name + "--";

            using (var writer = new StringWriter()) {
                while (true) {
                    var line = reader.ReadLine();
                    if (line != null) {
                        var isBoundaryTerminator = line.StartsWith(boundaryTerminator);
                        var isBoundaryStart = line == boundaryStart;

                        if (isBoundaryTerminator || isBoundaryStart) {
                            var content = writer.ToString();
                            parts.Add(content);
                            writer.Clear();

                            // last line or end of boundary
                            if (isBoundaryTerminator) {
                                break;
                            }

                            continue;
                        }
                    }

                    writer.WriteLine(line);
                }
            }

            return parts;
        }

        private void CommitHeaderField(LineInfo last)
        {
            var field = FieldRegistry.Instance.CreateField(last.FieldName);
            field.Deserialize(last.Text);
            Headers.Add(field);
        }

        public override string ToString()
        {
            return new EntityDebuggerProxy(this).ToString();
        }

        public static Entity CreateMultipartMixed()
        {
            return CreateBase64(ContentTypes.MultipartMixed);
        }

        public static Entity CreateMultipartAlternative()
        {
            return CreateBase64(ContentTypes.MultipartAlternative);
        }

        public static Entity CreateMultipartDigest()
        {
            return CreateBase64(ContentTypes.MultipartDigest);
        }

        public static Entity CreateTextPlain()
        {
            return CreateUtf8(MediaTypes.TextPlain, ContentTransferEncodings.QuotedPrintable, false);
        }

        public static Entity CreateTextHtml()
        {
            return CreateUtf8(MediaTypes.TextPlain, ContentTransferEncodings.QuotedPrintable, false);
        }

        public static Entity CreateUtf8(string contentType, string encoding, bool boundary)
        {
            var entity = Create(contentType, encoding, boundary);
            entity.ContentTypeHeaderField.Charset = Charsets.Utf8;
            return entity;
        }

        public static Entity Create(string contentType, string encoding, bool boundary)
        {
            var entity = new Entity();
            var contentTypeField = new ContentTypeHeaderField {MediaType = contentType};

            if (boundary) {
                contentTypeField.BoundaryName = Boundary.Create();
            }

            var contentTransferEncodingField = new ContentTransferEncodingHeaderField {Encoding = encoding};

            entity.Headers.Add(contentTypeField);
            entity.Headers.Add(contentTransferEncodingField);
            return entity;
        }

        public static Entity Create(string contentType, string encoding)
        {
            return Create(contentType, encoding, true);
        }

        public static Entity CreateBase64(string contentType)
        {
            return Create(contentType, ContentTransferEncodings.Base64);
        }

        public static Entity CreateMultipartRelated()
        {
            return CreateBase64(ContentTypes.MultipartRelated);
        }

        internal static Entity CreateMessageRfc822()
        {
            var entity = new Entity();
            var contentTypeField = new ContentTypeHeaderField {BoundaryName = Boundary.Create(), MediaType = ContentTypes.MessageRfc822, Charset = Charsets.Ansi};
            entity.Headers.Add(contentTypeField);
            return entity;
        }

        #region Debugger Type Proxy

        internal sealed class EntityDebuggerProxy
        {
            private readonly Entity _entity;

            public EntityDebuggerProxy(Entity entity)
            {
                _entity = entity;
            }

            public string ContentType
            {
                get { return _entity.HasContentType ? _entity.ContentTypeHeaderField.MediaType : string.Empty; }
            }

            public string Charset
            {
                get { return _entity.HasContentType ? _entity.ContentTypeHeaderField.Charset : string.Empty; }
            }

            public string Encoding
            {
                get { return _entity.HasContentTransferEncoding ? _entity.ContentTransferEncodingHeaderField.Encoding : string.Empty; }
            }

            public override string ToString()
            {
                return string.Format("ContentType = {0}, Charset = {1}, Encoding = {2}", ContentType, Charset, Encoding);
            }
        }

        #endregion
    }
}