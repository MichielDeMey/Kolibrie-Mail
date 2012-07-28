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
using Crystalbyte.Equinox.IO;
using Crystalbyte.Equinox.Mime;
using Crystalbyte.Equinox.Text;

namespace Crystalbyte.Equinox.Imap.Linq
{
    /// <summary>
    ///   This class will translate a LINQ expression tree into an IMAP SEARCH COMMAND QUERY. 
    ///   The IMAP search command reference can be found under http://tools.ietf.org/html/rfc3501#section-6.4.4
    /// </summary>
    internal sealed class ImapQueryTranslator : ExpressionVisitor
    {
        private readonly Stack<HeaderSearchPair> _headerSearchStack = new Stack<HeaderSearchPair>();
        private readonly SequenceSetStorage _sequenceNumberSet = new SequenceSetStorage();
        private readonly SequenceSetStorage _uidSet = new SequenceSetStorage();
        private readonly StringWriter _writer = new StringWriter();

        public SearchTranslationResult Translate(Expression expression)
        {
            _writer.Clear();

            AppendWord(SearchCommands.Search);

            Visit(expression);
            ProcessDeferredCommands();

            return new SearchTranslationResult
                       {
                           SearchCommand = _writer.ToString().TrimEnd(),
                           IsUid = _uidSet.IsTouched
                       };
        }

        /// <summary>
        ///   This method produces a single query string out of multiple CLR expressions.
        /// </summary>
        private void ProcessDeferredCommands()
        {
            ProcessDeferredUidCommand();
            ProcessDeferredSequenceNumberCommand();
            ProcessDeferredHeaderCommand();
        }

        private void ProcessDeferredSequenceNumberCommand()
        {
            if (_uidSet.IsTouched) {
                return;
            }

            var replacement = SearchCommands.Search + Characters.Space;

            var from = _sequenceNumberSet.Range.From;
            var to = _sequenceNumberSet.Range.To;
            var values = _sequenceNumberSet.Values;

            if (from < 0 && to < 0 && values.Count == 0) {
                return;
            }

            if (from > 0 || to > 0) {
                if (from < 0) {
                    replacement += Characters.Asterisk;
                } else {
                    var text = from.ToString();
                    replacement += text;
                }

                replacement += Characters.Colon;

                if (to < 0) {
                    replacement += Characters.Asterisk;
                } else {
                    var text = to.ToString();
                    replacement += text;
                }
            } else {
                replacement = values.Aggregate(replacement, (current, value) => current + (value.ToString() + Characters.Space));
            }

            _writer.Replace(SearchCommands.Search, replacement);
        }

        private void ProcessDeferredHeaderCommand()
        {
            while (_headerSearchStack.Count > 0) {
                var pair = _headerSearchStack.Pop();
                AppendWord(SearchCommands.Header);
                AppendQuotedText(pair.Name);
                AppendChar(Characters.Space);
                AppendQuotedText(pair.Value);
                AppendChar(Characters.Space);
            }
        }

        /// <summary>
        ///   This method merges multiple CLR expressions for the Uid property into a single SEARCH command.
        ///   For example "x => x.Uid (larger) 5 && x.Uid (smaller) 500"will be converted into "UID 5:500".
        /// </summary>
        private void ProcessDeferredUidCommand()
        {
            if (_sequenceNumberSet.IsTouched) {
                return;
            }

            var replacement = SearchCommands.Uid + Characters.Space + SearchCommands.Search + Characters.Space;

            var from = _uidSet.Range.From;
            var to = _uidSet.Range.To;
            var values = _uidSet.Values;

            if (from < 0 && to < 0 && values.Count == 0) {
                return;
            }

            replacement += SearchCommands.Uid + Characters.Space;

            if (from > 0 || to > 0) {
                if (from < 0) {
                    replacement += Characters.Asterisk;
                } else {
                    var text = from.ToString();
                    replacement += text;
                }

                replacement += Characters.Colon;

                if (to < 0) {
                    replacement += Characters.Asterisk;
                } else {
                    var text = to.ToString();
                    replacement += text;
                }
            } else {
                replacement = values.Aggregate(replacement, (current, value) => current + (value.ToString() + Characters.Space));
            }

            _writer.Replace(SearchCommands.Search, replacement);
        }

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            Arguments.VerifyNotNull(m.Arguments[0]);
            var name = m.Method.Name;
            switch (name) {
                case MemberNames.Where:
                    return VisitMethodCallWhere(m);
                case MemberNames.HasFlag:
                    return VisitMethodCallHasFlag(m);
                case MemberNames.Contains:
                    return VisitMethodCallContains(m);
                case MemberNames.Any:
                    break;
                default:
                    var message = string.Format("The method {0} is not supported.", name);
                    throw new NotSupportedException(message);
            }
            return base.VisitMethodCall(m);
        }

        private Expression VisitMethodCallContains(MethodCallExpression m)
        {
            var name = m.Method.Name;

            MemberExpression instance;
            ConstantExpression argument;

            if (m.Object == null) {
                // is static extension method
                if (m.Arguments[0].Type != typeof (IEnumerable<EmailContact>) && m.Arguments[1].Type != typeof (string)) {
                    var message = string.Format("The method {0} is not supported.", name);
                    throw new NotSupportedException(message);
                }

                instance = (MemberExpression) RemoveConvertExpression(m.Arguments[0]);
                argument = (ConstantExpression) RemoveConvertExpression(m.Arguments[1]);
            } else {
                if (m.Arguments[0].Type != typeof (string)) {
                    var message = string.Format("The method {0} is not supported.", name);
                    throw new NotSupportedException(message);
                }

                instance = (MemberExpression) RemoveConvertExpression(m.Object);
                argument = (ConstantExpression) RemoveConvertExpression(m.Arguments[0]);
            }

            if (argument.Type != typeof (string)) {
                var message = string.Format("The method {0} is not supported.", name);
                throw new NotSupportedException(message);
            }

            var value = (string) argument.Value;

            switch (instance.Member.Name) {
                case MemberNames.Name:
                    VisitMethodCallContainsOnNameProperty(value);
                    return m;
                case MemberNames.From:
                    VisitExtensionMethodCallContainsOnVirtualPropertyFrom(value);
                    return m;
                case MemberNames.To:
                    VisitExtensionMethodCallContainsOnVirtualPropertyTo(value);
                    return m;
                case MemberNames.Value:
                    VisitMethodCallContainsOnBodyProperty(value);
                    return m;
                case MemberNames.Text:
                    VisitMethodCallContainsOnTextProperty(value);
                    return m;
                case MemberNames.Keywords:
                    VisitMethodCallContainsOnKeywordsProperty(value);
                    return m;
                default:
                    var message = string.Format("The property {0} is not supported.", instance.Member.Name);
                    throw new NotSupportedException(message);
            }
        }

        private void VisitExtensionMethodCallContainsOnVirtualPropertyTo(string value)
        {
            AppendQuotedKeyValuePair(SearchCommands.To, value);
        }

        private void VisitExtensionMethodCallContainsOnVirtualPropertyFrom(string value)
        {
            AppendQuotedKeyValuePair(SearchCommands.From, value);
        }

        private void VisitMethodCallContainsOnKeywordsProperty(string value)
        {
            AppendWord(SearchCommands.Keyword);
            AppendWord(value);
        }

        private void VisitMethodCallContainsOnTextProperty(string value)
        {
            AppendQuotedKeyValuePair(SearchCommands.Text, value);
        }

        private void ValidateStackNotEmpty()
        {
            if (_headerSearchStack.Count == 0) {
                const string message = "Header stack must not be empty.";
                throw new ApplicationException(message);
            }
        }

        private void VisitMethodCallContainsOnBodyProperty(string value)
        {
            ValidateStackNotEmpty();
            var pair = _headerSearchStack.Peek();
            pair.Value = value;
        }

        private void VisitMethodCallContainsOnNameProperty(string value)
        {
            ValidateStackNotEmpty();
            var pair = _headerSearchStack.Peek();
            pair.Name = value;
        }

        private Expression VisitMethodCallHasFlag(MethodCallExpression m)
        {
            var instance = RemoveConvertExpression(m.Object);
            var argument = (ConstantExpression) RemoveConvertExpression(m.Arguments[0]);

            switch (instance.NodeType) {
                case ExpressionType.MemberAccess:
                    {
                        var access = (MemberExpression) instance;
                        var name = access.Member.Name;
                        switch (name) {
                            case MemberNames.Flags:
                                VisitMethodCallHasFlagOnFlagsProperty(argument);
                                return m;
                            default:
                                {
                                    var message = string.Format("The method HasFlags is not supported on member {0}.", name);
                                    throw new NotSupportedException(message);
                                }
                        }
                    }
                default:
                    {
                        var message = string.Format("The expression type {0} is not supported.", instance.NodeType);
                        throw new NotSupportedException(message);
                    }
            }
        }

        private void VisitMethodCallHasFlagOnFlagsProperty(ConstantExpression argument)
        {
            var value = (MessageFlags) argument.Value;
            switch (value) {
                case MessageFlags.Seen:
                    AppendWord(SearchCommands.Seen);
                    break;
                case MessageFlags.Recent:
                    AppendWord(SearchCommands.Recent);
                    break;
                case MessageFlags.Flagged:
                    AppendWord(SearchCommands.Flagged);
                    break;
                case MessageFlags.Draft:
                    AppendWord(SearchCommands.Draft);
                    break;
                case MessageFlags.Deleted:
                    AppendWord(SearchCommands.Deleted);
                    break;
                case MessageFlags.Answered:
                    AppendWord(SearchCommands.Answered);
                    break;
            }
        }

        private Expression VisitMethodCallWhere(MethodCallExpression m)
        {
            return base.VisitMethodCall(m);
        }

        protected override Expression VisitUnary(UnaryExpression u)
        {
            switch (u.NodeType) {
                case ExpressionType.Not:
                    VisitUnaryNot();
                    break;
            }
            return base.VisitUnary(u);
        }

        private void VisitUnaryNot()
        {
            AppendWord(SearchCommands.Not);
        }

        private void AppendQuotedText(string text)
        {
            _writer.Write(Characters.Quote);
            _writer.Write(text);
            _writer.Write(Characters.Quote);
        }

        private void AppendQuotedKeyValuePair(string key, string value)
        {
            AppendWord(key);
            AppendQuotedText(value);
            AppendChar(Characters.Space);
        }

        private void AppendChar(char text)
        {
            _writer.Write(text);
        }

        private void AppendWord(string word)
        {
            _writer.Write(word);
            _writer.Write(Characters.Space);
        }

        private Expression VisitAndDeferUidExpression(BinaryExpression expression)
        {
            var value = (int) ((ConstantExpression) expression.Right).Value;
            var range = _uidSet.Range;

            switch (expression.NodeType) {
                case ExpressionType.GreaterThan:
                    _uidSet.Range = Range.FromPair(value + 1, range.To);
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    _uidSet.Range = Range.FromPair(value, range.To);
                    break;
                case ExpressionType.LessThan:
                    _uidSet.Range = Range.FromPair(range.From, value - 1);
                    break;
                case ExpressionType.LessThanOrEqual:
                    _uidSet.Range = Range.FromPair(range.From, value);
                    break;
                case ExpressionType.Equal:
                    _uidSet.Values.Add(value);
                    break;
                default:
                    var message = string.Format("The method {0} is not supported.", expression.NodeType);
                    throw new NotSupportedException(message);
            }
            return expression;
        }

        /// <summary>
        ///   Translates the binary Or and OrElse operator into the IMAP equivalent.
        /// </summary>
        /// <param name = "b">The binary CLR expression.</param>
        /// <returns>The original expression.</returns>
        private Expression VisitOrElse(BinaryExpression b)
        {
            AppendWord(SearchCommands.Or);
            Visit(b.Left);
            Visit(b.Right);
            return b;
        }

        protected override Expression VisitMemberAccess(MemberExpression m)
        {
            var name = m.Member.Name;
            switch (name) {
                case MemberNames.IsNew:
                    return VisitMemberIsNew(m);
                case MemberNames.IsOld:
                    return VisitMemberIsOld(m);
                case MemberNames.Headers:
                    return VisitMemberCallHeaders(m);
                case MemberNames.Value:
                case MemberNames.Name:
                case MemberNames.Text:
                case MemberNames.Keywords:
                case MemberNames.Flags:
                    return base.VisitMemberAccess(m);
                default:
                    var message = string.Format("The method {0} is not supported.", name);
                    throw new NotSupportedException(message);
            }
        }

        private Expression VisitMemberCallHeaders(Expression m)
        {
            var pair = new HeaderSearchPair();
            _headerSearchStack.Push(pair);
            return m;
        }

        private Expression VisitMemberIsOld(Expression m)
        {
            AppendWord(SearchCommands.Not);
            AppendWord(SearchCommands.Recent);
            return m;
        }

        private Expression VisitMemberIsNew(Expression m)
        {
            AppendWord(SearchCommands.New);
            return m;
        }

        private static Expression RemoveConvertExpression(Expression expression)
        {
            return expression.NodeType == ExpressionType.Convert ? ((UnaryExpression) expression).Operand : expression;
        }

        private Expression VisitCompareExpressions(BinaryExpression b)
        {
            var left = RemoveConvertExpression(b.Left);
            var leftType = left.NodeType;

            switch (leftType) {
                case ExpressionType.MemberAccess:
                    {
                        var access = (MemberExpression) left;
                        var name = access.Member.Name;
                        switch (name) {
                            case MemberNames.Date:
                                return VisitDateCompareExpression(b);
                            case MemberNames.InternalDate:
                                return VisitInternalDateCompareExpression(b);
                            case MemberNames.Uid:
                                return VisitAndDeferUidExpression(b);
                            case MemberNames.SequenceNumber:
                                return VisitAndDeferSequenceNumberExpression(b);
                            default:
                                var message = string.Format("The member {0} is not supported.", name);
                                throw new NotSupportedException(message);
                        }
                    }
                default:
                    var msg = string.Format("The expression type {0} is not supported.", leftType);
                    throw new NotSupportedException(msg);
            }
        }

        private Expression VisitAndDeferSequenceNumberExpression(BinaryExpression b)
        {
            var value = (int) ((ConstantExpression) b.Right).Value;
            var range = _sequenceNumberSet.Range;

            switch (b.NodeType) {
                case ExpressionType.GreaterThan:
                    _sequenceNumberSet.Range = Range.FromPair(value + 1, range.To);
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    _sequenceNumberSet.Range = Range.FromPair(value, range.To);
                    break;
                case ExpressionType.LessThan:
                    _sequenceNumberSet.Range = Range.FromPair(range.From, value - 1);
                    break;
                case ExpressionType.LessThanOrEqual:
                    _sequenceNumberSet.Range = Range.FromPair(range.From, value);
                    break;
                case ExpressionType.Equal:
                    _sequenceNumberSet.Values.Add(value);
                    break;
                default:
                    var message = string.Format("The method {0} is not supported.", b.NodeType);
                    throw new NotSupportedException(message);
            }
            return b;
        }

        private Expression VisitInternalDateCompareExpression(BinaryExpression b)
        {
            var value = (DateTime) ((ConstantExpression) b.Right).Value;
            var date = value.ToMimeDateString();
            var type = b.NodeType;

            switch (type) {
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                    AppendWord(SearchCommands.Since);
                    AppendWord(date);
                    return b;
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                    AppendWord(SearchCommands.Before);
                    AppendWord(date);
                    return b;
                case ExpressionType.Equal:
                    AppendWord(SearchCommands.On);
                    AppendWord(date);
                    return b;
                case ExpressionType.NotEqual:
                    AppendWord(SearchCommands.Not);
                    AppendWord(SearchCommands.On);
                    AppendWord(date);
                    return b;
                default:
                    {
                        var message = string.Format("The expression type {0} is not supported.", type);
                        throw new NotSupportedException(message);
                    }
            }
        }

        private Expression VisitDateCompareExpression(BinaryExpression b)
        {
            var value = (DateTime) ((ConstantExpression) b.Right).Value;
            var date = value.ToMimeDateString();
            var type = b.NodeType;

            switch (type) {
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                    AppendWord(SearchCommands.SentSince);
                    AppendWord(date);
                    return b;
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                    AppendWord(SearchCommands.SentBefore);
                    AppendWord(date);
                    return b;
                case ExpressionType.Equal:
                    AppendWord(SearchCommands.SentOn);
                    AppendWord(date);
                    return b;
                case ExpressionType.NotEqual:
                    AppendWord(SearchCommands.Not);
                    AppendWord(SearchCommands.SentOn);
                    AppendWord(date);
                    return b;
                default:
                    {
                        var message = string.Format("The expression type {0} is not supported.", type);
                        throw new NotSupportedException(message);
                    }
            }
        }

        protected override Expression VisitBinary(BinaryExpression b)
        {
            switch (b.NodeType) {
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.NotEqual:
                case ExpressionType.Equal:
                    return VisitCompareExpressions(b);
                case ExpressionType.OrElse:
                    return VisitOrElse(b);
            }
            return base.VisitBinary(b);
        }
    }
}