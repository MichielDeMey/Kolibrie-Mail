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

namespace Crystalbyte.Equinox.Mime
{
    /// <summary>
    ///   Many media types which could be usefully transported via email are
    ///   represented, in their "natural" format, as 8bit character or binary
    ///   data.  Such data cannot be transmitted over some transfer protocols.
    ///   For example, RFC 821 (SMTP) restricts mail messages to 7bit US-ASCII
    ///   data with lines no longer than 1000 characters including any trailing
    ///   CRLF line separator.
    ///   It is necessary, therefore, to define a standard mechanism for
    ///   encoding such data into a 7bit short line format.  Proper labelling
    ///   of unencoded material in less restrictive formats for direct use over
    ///   less restrictive transports is also desireable.  This document
    ///   specifies that such encodings will be indicated by a new "Content-
    ///   Transfer-Encoding" header field.  This field has not been defined by
    ///   any previous standard.
    ///   http://tools.ietf.org/html/rfc2045#section-6
    /// </summary>
    public sealed class ContentTransferEncodingHeaderField : HeaderField
    {
        public ContentTransferEncodingHeaderField()
        {
            Name = FieldNames.ContentTransferEncoding;
        }

        public string Encoding
        {
            get { return Value; }
            set { Value = value; }
        }
    }
}