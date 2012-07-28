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
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using Crystalbyte.Equinox.Imap.Processing;
using Crystalbyte.Equinox.IO;
using Crystalbyte.Equinox.Text;

namespace Crystalbyte.Equinox.Imap.Linq
{
    /// <summary>
    ///   This class creates the fetch command string from a LINQ expression tree.
    ///   In addition it generates a surrogate expression tree while traversing
    ///   the original that will bind the responses to the given properties once all data has been received.
    /// </summary>
    internal sealed class ImapFetchQueryTranslator : ExpressionVisitor
    {
        private readonly List<string> _headerRequests = new List<string>();
        private readonly ResponseProcessor _processor = new ResponseProcessor();
        private readonly StringWriter _writer = new StringWriter();
        private bool _isSingleMemberAccess;

        public bool IsUid { get; set; }
        public bool UsePeek { get; set; }

        private void AppendFetchPrefix(string messageSet)
        {
            if (IsUid) {
                AppendWord(SearchCommands.Uid);
            }
            AppendWord("FETCH");
            AppendWord(messageSet);
            AppendChar('(');
        }

        private void AppendFetchPostfix()
        {
            _writer.TrimEnd(Characters.Space);
            AppendChar(')');
        }

        public string Translate(LambdaExpression expression, string messageSet, out ResponseProcessor info)
        {
            info = _processor;

            DetermineSelectionType(expression);
            AppendFetchPrefix(messageSet);
            AppendFetchBody(expression);
            AppendFetchPostfix();

            if (_isSingleMemberAccess) {
                CreateSingleMemberAccessSurrogate(expression);
            }

            return _writer.ToString();
        }

        private void CreateSingleMemberAccessSurrogate(LambdaExpression expression)
        {
            var returnType = expression.ReturnType;
            var identifier = _processor.Items.First().Identifier;

            var dataAccess = CreateSurrogateExpression(returnType, identifier);
            var lambda = Expression.Lambda(dataAccess, Expression.Parameter(typeof (IMessageQueryable)));

            _processor.Expression = lambda;
        }

        private void DetermineSelectionType(LambdaExpression expression)
        {
            _isSingleMemberAccess = expression.Body.NodeType != ExpressionType.MemberInit;
        }

        private void AppendFetchBody(Expression expression)
        {
            var surrogate = Visit(expression);
            StoreReformedExpression(surrogate);

            if (_headerRequests.Count > 0) {
                CompressAndAppendHeaders();
            }

            _writer.TrimEnd(Characters.Space);
        }

        private void StoreReformedExpression(Expression surrogate)
        {
            _processor.Expression = surrogate;
        }

        private void CompressAndAppendHeaders()
        {
            var headers = FormatHeaderFieldsCommand(_headerRequests);
            var envelope = FormatBodyCommand(headers, UsePeek);
            AppendWord(envelope);
        }

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            if (m.Method.Name == MemberNames.Parts) {
                var constant = (ConstantExpression) m.Arguments[0];
                var dynamic = constant.Value as string;

                var item = CreateResponseItemFromFetchToken(dynamic);
                StoreResponseItem(item);

                if (!string.IsNullOrEmpty(dynamic)) {
                    dynamic = dynamic.ToUpper();
                }

                var command = FormatBodyCommand(dynamic, UsePeek);
                AppendWord(command);
            }

            return base.VisitMethodCall(m);
        }

        private static ResponseItem CreateResponseItemFromFetchToken(string token)
        {
            token = token.ToUpper();
            var command = FormatBodyResponseIdentifier(token);
            var item = new ResponseItem {Identifier = command};

            if (token.EndsWith("HEADER") || token.EndsWith("MIME")) {
                item.SectionParser = new HeaderCollectionParser();
            } else {
                item.SectionParser = new TextParser();
            }
            item.SectionReader = new DynamicSectionReader(command);

            return item;
        }

        protected override MemberBinding VisitBinding(MemberBinding binding)
        {
            // we need to visit the original expression tree first in order to collect all
            // necessary informations
            base.VisitBinding(binding);

            // now we replace the original member binding for our 
            // GetValues call to the ResponseCatalogue
            if (binding is MemberAssignment) {
                var assignment = (MemberAssignment) binding;

                var type = assignment.Expression.Type;
                var identifier = _processor.Items.Last().Identifier;
                var expression = CreateSurrogateExpression(type, identifier);
                var surrogate = assignment.Update(expression);
                return surrogate;
            }

            throw new NotSupportedException();
        }

        private Expression CreateSurrogateExpression(Type returnType, string identifier)
        {
            var methodInfo = typeof (ResponseCatalogue).GetMethod("GetValue");
            var expression = Expression.Convert(Expression.Call(Expression.Constant(_processor.Catalogue), methodInfo, Expression.Constant(identifier)), returnType);
            return expression;
        }

        protected override Expression VisitMemberAccess(MemberExpression m)
        {
            var fetchable = GetFetchableAttribute(m);

            if (fetchable.IsHeader) {
                // header commands will be merged to reduce net traffic
                StoreHeaderRequest(fetchable.Identifier);
            } else {
                // the sequence number must not be requested, it always comes whether we want or not.
                if (!fetchable.IsSequenceNumber) {
                    // other commands can't be merged, therefor they can be added without delay.
                    AppendWord(fetchable.Identifier);
                }
            }

            StoreResponseItem(fetchable);

            return base.VisitMemberAccess(m);
        }

        private void StoreHeaderRequest(string name)
        {
            _headerRequests.Add(name);
        }

        private void StoreResponseItem(ResponseItem item)
        {
            _processor.Items.Add(item);
        }

        private void StoreResponseItem(FetchableAttribute fetchable, string dynamicIdentifier = "")
        {
            var reader = Activator.CreateInstance(fetchable.ReaderType) as ISectionReader;
            var parser = Activator.CreateInstance(fetchable.ParserType) as ISectionParser;
            var identifier = string.IsNullOrEmpty(dynamicIdentifier) ? fetchable.Identifier : dynamicIdentifier;

            var item = new ResponseItem {Identifier = identifier, IsInline = fetchable.IsInline, SectionParser = parser, SectionReader = reader};

            StoreResponseItem(item);
        }

        private static FetchableAttribute GetFetchableAttribute(MemberExpression m)
        {
            var type = m.Expression.Type;
            var property = type.GetProperty(m.Member.Name);
            return (FetchableAttribute) (property.GetCustomAttributes(typeof (FetchableAttribute), true)[0]);
        }

        private void AppendChar(char text)
        {
            _writer.Write(text);
        }

        private void AppendText(string text)
        {
            _writer.Write(text);
        }

        private void AppendWord(string word)
        {
            AppendText(word);
            _writer.Write(Characters.Space);
        }

        private static string FormatBodyCommand(string value, bool usePeek)
        {
            return string.Format(usePeek ? "BODY.PEEK[{0}]" : "BODY[{0}]", value);
        }

        private static string FormatBodyResponseIdentifier(string value)
        {
            return string.Format("BODY[{0}]", value);
        }

        private static string FormatHeaderFieldsCommand(IEnumerable<string> headers)
        {
            using (var sw = new StringWriter()) {
                sw.Write("HEADER.FIELDS (");
                foreach (var header in headers) {
                    sw.Write(header);
                    sw.Write(Characters.Space);
                }
                sw.TrimEnd(Characters.Space);
                sw.Write(")");
                return sw.ToString();
            }
        }
    }
}