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
using System.IO;
using System.Text.RegularExpressions;
using Crystalbyte.Equinox.IO;
using Crystalbyte.Equinox.Mime.Text;
using Crystalbyte.Equinox.Text;

namespace Crystalbyte.Equinox.Imap.Processing
{
    internal sealed class BodyStructReader : ISectionReader
    {
        #region ISectionReader Members

        public string ReadSection(ImapResponseReader reader)
        {
            var text = reader.CurrentLine;
            var sn = Regex.Match(text, @"\d+").Value;


            using (var sw = new StringWriter()) {
                using (var sr = new StringReader(text)) {
                    var stack = new Stack<char>();

                    var isStarted = false;
                    var index = text.IndexOf("BODYSTRUCTURE");
                    var buffer = new char[index + 1];
                    sr.Read(buffer, 0, index);

                    while (true) {
                        if (isStarted) {
                            sw.Write(buffer[0]);
                        }

                        var count = sr.Read(buffer, 0, 1);

                        // end of string
                        if (count == 0) {
                            break;
                        }

                        // matching brace found
                        if (isStarted && stack.Count == 0) {
                            break;
                        }

                        if (buffer[0] == Characters.RoundOpenBracket) {
                            stack.Push(buffer[0]);
                            isStarted = true;
                            continue;
                        }

                        if (buffer[0] == Characters.RoundClosedBracket) {
                            stack.Pop();
                            continue;
                        }
                    }

                    // is a size appended to indicate multilined data? {####}
                    var sizeMatch = Regex.Match(reader.CurrentLine, @"\{\d+\}$");
                    if (sizeMatch.Success) {
                        // need to remove {####} from end of string
                        sw.RemoveLast(sizeMatch.Value.Length);

                        // Usually we could determine the amount of data to read from the size parameter at the end of the line
                        // unfortunately here the number contained seems to be completely arbitrary since it never fits, coming from 1und1 its always 107, no matter what follows.
                        // The 'only' thing I can be sure of when determining the amount of data to append is that it has to end with a closing bracket, no matter what.
                        while (true) {
                            reader.ReadNextLine();
                            var line = reader.CurrentLine;
                            sizeMatch = Regex.Match(reader.CurrentLine, @"\{\d+\}$");

                            // cut trailing {###} expression if necessary
                            var normalizedLine = sizeMatch.Success ? line.Substring(0, line.Length - sizeMatch.Value.Length) : line;

                            // decode line since its usually encoded using QP or Base64
                            normalizedLine = TransferEncoder.DecodeHeaderIfNecessary(normalizedLine);

                            // append line to regular body struct
                            sw.Write(normalizedLine);
                            if (reader.CurrentLine.EndsWith(")")) {
                                break;
                            }
                        }
                    }

                    // we silently append the sequence number to the end of the body structure,
                    // the parser will ignore it, but we can read it later without having to split or change this string
                    sw.Write(Characters.Space);
                    sw.Write(sn);
                    return sw.ToString();
                }
            }
        }

        public bool CanRead(string value)
        {
            return value.Contains("BODYSTRUCTURE");
        }

        #endregion
    }
}