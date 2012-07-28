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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Crystalbyte.Equinox.Imap.Commands;
using Crystalbyte.Equinox.Text;

namespace Crystalbyte.Equinox.Imap.Linq
{
    internal sealed class ImapMessageQueryProvider : QueryProvider
    {
        private readonly ImapClient _client;

        public ImapMessageQueryProvider(ImapClient client)
        {
            _client = client;
        }

        public override object Execute(Expression expression)
        {
            // reduce expressions to constants where possible
            expression = Evaluator.PartialEval(expression);

            var crawler = new ExpressionCrawler();
            var searchExpression = crawler.FindSearchLambda(expression);
            var fetchExpression = crawler.FindFetchLamda(expression);

            // the search string sequence
            string sequence;

            var searchTranslationResult = new ImapQueryTranslator().Translate(searchExpression);

            var isSearchRequired = searchTranslationResult.ValidateSearchNecessity();
            if (isSearchRequired) {
                var searchCommand = new ImapCommand(searchTranslationResult.SearchCommand);
                var response = _client.SendAndReceive(searchCommand);

                var success = TryExtractIds(response, out sequence);
                if (!success) {
                    // no messages match the search criteria therefor we don't need to fetch anything
                    // we return an empty list
                    return Activator.CreateInstance(typeof (List<>).MakeGenericType(fetchExpression.ReturnType), null);
                }
            } else {
                sequence = searchTranslationResult.ConvertCommandToSequence();
            }


            // There are no id's if no message matches the search criterias.
            var fetchTranslator = new ImapFetchQueryTranslator {IsUid = searchTranslationResult.IsUid, UsePeek = _client.UsePeek};

            ResponseProcessor processor;
            var fetchString = fetchTranslator.Translate(fetchExpression, sequence, out processor);
            var fetchCommand = new ImapCommand(fetchString);
            var fetchResponse = _client.SendAndReceive(fetchCommand);

            return Activator.CreateInstance(
                typeof (ImapQueryResponseReader<>).MakeGenericType(new[] {fetchExpression.ReturnType}),
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new object[] {fetchResponse, processor},
                CultureInfo.CurrentCulture);
        }

        private static bool TryExtractIds(ImapResponseReader reader, out string messageSet)
        {
            var result = false;
            using (var sw = new StringWriter()) {
                while (!reader.IsCompleted) {
                    var values = reader.CurrentLine.Split(Characters.Space);
                    foreach (var value in
                        from value in values let isNumeric = value.IsNumeric() where isNumeric select value) {
                        sw.Write(value);
                        sw.Write(Characters.Comma);
                        result = true;
                    }
                    reader.ReadNextLine();
                }

                messageSet = sw.ToString().TrimEnd(Characters.Comma);
                return result;
            }
        }
    }
}