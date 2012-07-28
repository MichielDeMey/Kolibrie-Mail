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
using Crystalbyte.Equinox.Mime.Collections;

namespace Crystalbyte.Equinox.Imap.Processing.Advanced
{
    internal static class StringExtensions
    {
        public static string TrimQuotes(this string input)
        {
            return input.Trim('\"');
        }
    }

    internal abstract class Part
    {
        protected Part()
        {
            Children = new List<Part>();
        }

        public IList<Part> Children { get; private set; }
        public Part Parent { get; set; }
    }

    internal sealed class BodyPart : Part
    {
        public BodyPart()
        {
            Parameters = new ParameterDictionary();
            Extensions = new List<string>();
            EnvelopeBounds = new int[2];
        }

        public string BodyType { get; set; }
        public string SubType { get; set; }
        public IDictionary<string, string> Parameters { get; private set; }
        public string BodyId { get; set; }
        public string BodyDescription { get; set; }
        public string BodyEncoding { get; set; }
        public string BodySize { get; set; }
        public string TextLines { get; set; }
        public int[] EnvelopeBounds { get; set; }
        public IList<string> Extensions { get; private set; }
    }

    internal sealed class MultiPart : Part
    {
        public MultiPart()
        {
            Extensions = new List<string>();
        }


        public string Type { get; set; }
        public IList<string> Extensions { get; private set; }
    }

    internal class Parser
    {
        public const int _EOF = 0;
        public const int _string = 1;
        public const int _number = 2;
        public const int maxT = 6;

        private const bool T = true;
        private const bool x = false;
        private const int minErrDist = 2;

        private static readonly bool[,] set = {{T, x, x, x, x, x, x, x}, {x, T, x, T, T, T, x, x}};

        private readonly Stack<string> _paramKeyStack = new Stack<string>();
        private readonly Stack<Part> _stack = new Stack<Part>();
        private int errDist = minErrDist;

        public Errors errors;

        public Token la; // lookahead token
        public Scanner scanner;
        public Token t; // last recognized token

        public Parser(Scanner scanner)
        {
            this.scanner = scanner;
            errors = new Errors();
        }

        public Part Root { get; set; }


        private ParamListType CurrentListType { get; set; }

        private BodyPart CurrentBodyPart
        {
            get { return (BodyPart) _stack.Peek(); }
        }

        private MultiPart CurrentMultiPart
        {
            get { return (MultiPart) _stack.Peek(); }
        }

        private bool TrySaveToParent(Part part, out Part parent)
        {
            if (_stack.Count == 0) {
                parent = null;
                return false;
            }
            var parent_ = _stack.Peek();
            parent_.Children.Add(part);
            parent = parent_;
            return true;
        }

        private void PushPart(Part part)
        {
            if (_stack.Count == 0) {
                Root = part;
            }
            _stack.Push(part);
        }

        private bool IsBodyPart()
        {
            var c = scanner.Peek();
            return c.val != "(";
        }

        // checks whether the current body type is a nested message
        private bool IsRfc822()
        {
            return CurrentBodyPart.BodyType.ToLower() == "message" && CurrentBodyPart.SubType.ToLower() == "rfc822";
        }


        private void SynErr(int n)
        {
            if (errDist >= minErrDist) {
                errors.SynErr(la.line, la.col, n);
            }
            errDist = 0;
        }

        public void SemErr(string msg)
        {
            if (errDist >= minErrDist) {
                errors.SemErr(t.line, t.col, msg);
            }
            errDist = 0;
        }

        private void Get()
        {
            for (;;) {
                t = la;
                la = scanner.Scan();
                if (la.kind <= maxT) {
                    ++errDist;
                    break;
                }

                la = t;
            }
        }

        private void Expect(int n)
        {
            if (la.kind == n) {
                Get();
            } else {
                SynErr(n);
            }
        }

        private bool StartOf(int s)
        {
            return set[s, la.kind];
        }

        private void ExpectWeak(int n, int follow)
        {
            if (la.kind == n) {
                Get();
            } else {
                SynErr(n);
                while (!StartOf(follow)) {
                    Get();
                }
            }
        }


        private bool WeakSeparator(int n, int syFol, int repFol)
        {
            var kind = la.kind;
            if (kind == n) {
                Get();
                return true;
            } else if (StartOf(repFol)) {
                return false;
            } else {
                SynErr(n);
                while (!(set[syFol, kind] || set[repFol, kind] || set[0, kind])) {
                    Get();
                    kind = la.kind;
                }
                return StartOf(syFol);
            }
        }


        private void IBS()
        {
            BodyPart();
        }

        private void BodyPart()
        {
            if (IsBodyPart()) {
                Expect(3);
                Console.WriteLine("Entering terminal part ...");
                var part = new BodyPart();
                Part parent;
                var success = TrySaveToParent(part, out parent);
                if (success) {
                    part.Parent = parent;
                }
                PushPart(part);

                Field();
                CurrentBodyPart.BodyType = t.val.TrimQuotes();
                Field();
                CurrentBodyPart.SubType = t.val.TrimQuotes();
                CurrentListType = ParamListType.BodyPart;
                ParamList();
                Field();
                CurrentBodyPart.BodyId = t.val.TrimQuotes();
                CurrentListType = ParamListType.Other;
                Field();
                CurrentBodyPart.BodyDescription = t.val.TrimQuotes();
                Field();
                CurrentBodyPart.BodyEncoding = t.val.TrimQuotes();
                Expect(2);
                CurrentBodyPart.BodySize = t.val.TrimQuotes();
                while (la.kind == 2) {
                    Get();
                }
                CurrentBodyPart.TextLines = t.val.TrimQuotes();
                BodyPartAppendix();
                Expect(4);
                Console.WriteLine("Leaving terminal part.");
                _stack.Pop();
            } else if (la.kind == 3) {
                MultiPart();
            } else {
                SynErr(7);
            }
        }

        private void Envelope()
        {
            Expect(3);
            CurrentBodyPart.EnvelopeBounds[0] = t.charPos;
            Field();
            Field();
            ContactList();
            ContactList();
            ContactList();
            ContactList();
            ContactList();
            ContactList();
            Field();
            Field();
            Expect(4);
            CurrentBodyPart.EnvelopeBounds[1] = t.charPos;
        }

        private void Field()
        {
            if (la.kind == 5) {
                Get();
            } else if (la.kind == 1) {
                Get();
            } else {
                SynErr(8);
            }
        }

        private void ContactList()
        {
            if (la.kind == 3) {
                Get();
                while (la.kind == 3) {
                    Get();
                    Field();
                    Field();
                    Field();
                    Field();
                    Expect(4);
                }
                Expect(4);
            } else if (la.kind == 5) {
                Get();
            } else {
                SynErr(9);
            }
        }

        private void ParamList()
        {
            Expect(3);
            while (la.kind == 1 || la.kind == 5) {
                Field();
                if (CurrentListType != ParamListType.Other) {
                    _paramKeyStack.Push(t.val.TrimQuotes());
                }

                FieldOrParList();
                if (CurrentListType == ParamListType.BodyPart) {
                    var key = _paramKeyStack.Pop();
                    CurrentBodyPart.Parameters.Add(key, t.val.TrimQuotes());
                }
            }
            Expect(4);
        }

        private void FieldOrParList()
        {
            if (la.kind == 1 || la.kind == 5) {
                Field();
            } else if (la.kind == 3) {
                ParamList();
            } else {
                SynErr(10);
            }
        }

        private void BodyPartAppendix()
        {
            if (IsRfc822()) {
                Envelope();
                BodyPart();
                Expect(2);
                CurrentBodyPart.TextLines = t.val.TrimQuotes();
                while (la.kind == 1 || la.kind == 3 || la.kind == 5) {
                    CurrentListType = ParamListType.BodyPart;
                    FieldOrParList();
                    CurrentBodyPart.Extensions.Add(t.val.TrimQuotes());
                    CurrentListType = ParamListType.Other;
                }
            } else if (StartOf(1)) {
                while (la.kind == 1 || la.kind == 3 || la.kind == 5) {
                    FieldOrParList();
                }
            } else {
                SynErr(11);
            }
        }

        private void MultiPart()
        {
            Expect(3);
            Console.WriteLine("Entering multipart ...");
            var part = new MultiPart();
            Part parent;
            var success = TrySaveToParent(part, out parent);
            if (success) {
                part.Parent = parent;
            }
            PushPart(part);

            BodyPart();
            while (la.kind == 3) {
                BodyPart();
            }
            Field();
            CurrentMultiPart.Type = t.val.TrimQuotes();
            while (la.kind == 1 || la.kind == 3 || la.kind == 5) {
                CurrentListType = ParamListType.MultiPart;
                FieldOrParList();
                CurrentMultiPart.Extensions.Add(t.val.TrimQuotes());
                CurrentListType = ParamListType.Other;
            }
            Expect(4);
            Console.WriteLine("Leaving multipart.");
            _stack.Pop();
        }


        public void Parse()
        {
            la = new Token();
            la.val = "";
            Get();
            IBS();
            Expect(0);
        }

        #region Nested type: ParamListType

        internal enum ParamListType
        {
            Other,
            BodyPart,
            MultiPart
        }

        #endregion
    }

    // end Parser


    internal class Errors
    {
        public int count; // number of errors detected
        public string errMsgFormat = "-- line {0} col {1}: {2}"; // 0=line, 1=column, 2=text
        public TextWriter errorStream = Console.Out; // error messages go to this stream

        public virtual void SynErr(int line, int col, int n)
        {
            string s;
            switch (n) {
                case 0:
                    s = "EOF expected";
                    break;
                case 1:
                    s = "string expected";
                    break;
                case 2:
                    s = "number expected";
                    break;
                case 3:
                    s = "\"(\" expected";
                    break;
                case 4:
                    s = "\")\" expected";
                    break;
                case 5:
                    s = "\"nil\" expected";
                    break;
                case 6:
                    s = "??? expected";
                    break;
                case 7:
                    s = "invalid BodyPart";
                    break;
                case 8:
                    s = "invalid Field";
                    break;
                case 9:
                    s = "invalid ContactList";
                    break;
                case 10:
                    s = "invalid FieldOrParList";
                    break;
                case 11:
                    s = "invalid BodyPartAppendix";
                    break;

                default:
                    s = "error " + n;
                    break;
            }
            errorStream.WriteLine(errMsgFormat, line, col, s);
            count++;
        }

        public virtual void SemErr(int line, int col, string s)
        {
            errorStream.WriteLine(errMsgFormat, line, col, s);
            count++;
        }

        public virtual void SemErr(string s)
        {
            errorStream.WriteLine(s);
            count++;
        }

        public virtual void Warning(int line, int col, string s)
        {
            errorStream.WriteLine(errMsgFormat, line, col, s);
        }

        public virtual void Warning(string s)
        {
            errorStream.WriteLine(s);
        }
    }

    // Errors


    internal class FatalError : Exception
    {
        public FatalError(string m)
            : base(m) {}
    }
}