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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace Crystalbyte.Equinox.Imap.Linq
{
    internal sealed class ImapQueryResponseReader<T> : IEnumerable<T>
    {
        private readonly ImapResponseReader _reader;
        private readonly ResponseProcessor _responseInfo;

        public ImapQueryResponseReader(ImapResponseReader reader, ResponseProcessor responseInfo)
        {
            Arguments.VerifyNotNull(reader);
            Arguments.VerifyNotNull(responseInfo);

            _reader = reader;
            _responseInfo = responseInfo;
        }

        public bool IsConsumed { get; private set; }

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator()
        {
            CheckConsumption();
            return new ImapResponseEnumerator(_reader, _responseInfo);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        private void CheckConsumption()
        {
            if (IsConsumed) {
                throw new InvalidOperationException("Item is already consumed, iteration is only possible once.");
            }
            IsConsumed = true;
        }

        #region Nested type: ImapResponseEnumerator

        private class ImapResponseEnumerator : IEnumerator<T>
        {
            private readonly ImapResponseReader _reader;
            private readonly ResponseProcessor _responseInfo;

            public ImapResponseEnumerator(ImapResponseReader reader, ResponseProcessor responseInfo)
            {
                Arguments.VerifyNotNull(reader);
                Arguments.VerifyNotNull(responseInfo);

                _reader = reader;
                _responseInfo = responseInfo;
            }

            #region IEnumerator<T> Members

            public T Current { get; private set; }

            public void Dispose()
            {
                // nothing to dispose here
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            public bool MoveNext()
            {
                if (_reader.IsCompleted) {
                    return false;
                }

                var pending = FillResponseCatalogue();
                if (pending) {
                    var expression = Expression.Lambda(_responseInfo.Expression);
                    var select = (Func<Func<IMessageQueryable, T>>) expression.Compile();
                    Current = select()(null);
                    return true;
                }
                return false;
            }

            public void Reset()
            {
                const string message = "An external connection cannot be reset.";
                throw new NotSupportedException(message);
            }

            #endregion

            private void SkipIrrelevantLines()
            {
                var line = _reader.CurrentLine;
                // skip lines that hold no informations
                while (true) {
                    if (!string.IsNullOrEmpty(line) || line != "(" || line != ")") {
                        break;
                    }
                    _reader.ReadNextLine();
                }
            }

            private bool FillResponseCatalogue()
            {
                _responseInfo.Catalogue.Clear();
                var itemsToProcess = _responseInfo.Items.Count;
                var processed = new List<ResponseItem>();
                var currentSequenceNumber = 0;
                var commitPending = false;

                while (processed.Count != itemsToProcess) {
                    var misses = 0;

                    SkipIrrelevantLines();

                    // we are done
                    if (_reader.IsCompleted) {
                        return commitPending;
                    }

                    if (_reader.IsUntagged) {
                        commitPending = true;
                        var lastSequenceNumber = currentSequenceNumber;
                        var match = Regex.Match(_reader.CurrentLine, @"\d+");
                        if (match.Success) {
                            currentSequenceNumber = int.Parse(match.Value);
                            // we have entered the next response start line and need to commit the last
                            if (lastSequenceNumber != currentSequenceNumber && lastSequenceNumber != 0) {
                                return true;
                            }
                        }
                    }

                    // we need to sort all items so that multiline responses will be processed last,
                    // otherwise it would be possible to skip a response which is queued further down the line
                    // by requesting new lines to soon.
                    var pendingItems = _responseInfo.Items.Where(item => !processed.Contains(item)).OrderByDescending(x => x.IsInline).ToList();

                    // Once a response item has been processed we must not read the next line for multiple inline responses
                    // can be contained within the same line.
                    // When no inline response can be matched we continue to the multi line responses.
                    // Once a multi line response has been processed we must begin anew with all yet unprocessed inline responses.
                    foreach (var item in pendingItems) {
                        var canRead = item.SectionReader.CanRead(_reader.CurrentLine);
                        if (canRead) {
                            var section = item.SectionReader.ReadSection(_reader);
                            try {
                                var value = item.SectionParser.Parse(section);
                                _responseInfo.Catalogue.Add(item.Identifier, value);
                            }
                            catch (Exception) {
                                var message = string.Format("Parser error, skipping item {0}", item.Identifier);
                                Debug.WriteLine(message);
                            }


                            processed.Add(item);

                            // we need to reset the loop, so inline arguments will be at the top again
                            if (!item.IsInline) {
                                break;
                            }
                        } else {
                            misses++;
                        }
                    }


                    // line is milked dry
                    if (misses == pendingItems.Count) {
                        _reader.ReadNextLine();
                    }
                }

                // if all data was stored in a single line
                // we need to move forward or everything will repeat on the same line
                if (!_reader.IsCompleted) {
                    _reader.ReadNextLine();
                }

                return true;
            }
        }

        #endregion
    }
}