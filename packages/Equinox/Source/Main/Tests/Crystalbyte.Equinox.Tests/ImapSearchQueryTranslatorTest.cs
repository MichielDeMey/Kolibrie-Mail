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
using System.Linq;
using System.Linq.Expressions;
using Crystalbyte.Equinox.Imap;
using Crystalbyte.Equinox.Imap.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Crystalbyte.Equinox.Tests
{
    ///<summary>
    ///  This is a test class for ImapQueryTranslatorTest and is intended
    ///  to contain all ImapQueryTranslatorTest Unit Tests
    ///</summary>
    [TestClass]
    public class ImapSearchQueryTranslatorTest
    {
        ///<summary>
        ///  Gets or sets the test context which provides
        ///  information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }

        #region Additional test attributes

        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //

        #endregion

        ///<summary>
        ///  A test for Translate
        ///</summary>
        [TestMethod]
        public void TranslateTestSearchSeen()
        {
            ImapQueryTranslator target;
            Expression expression;

            using (var client = new ImapClient()) {
                target = new ImapQueryTranslator();
                expression = client.Messages.Where(x => x.Flags.HasFlag(MessageFlags.Seen)).Expression;
            }

            const string expected = "SEARCH SEEN";
            var result = target.Translate(expression);
            Assert.IsFalse(result.IsUid);
            Assert.AreEqual(expected, result.SearchCommand);
        }

        ///<summary>
        ///  A test for Translate
        ///</summary>
        [TestMethod]
        public void TranslateNegativeOperatorSeenFlags()
        {
            using (var client = new ImapClient()) {
                var translator = new ImapQueryTranslator();
                var expression = client.Messages.Where(x => x.Flags == MessageFlags.Seen).Expression;
                SearchTranslationResult result = null;
                try {
                    result = translator.Translate(expression);
                    Assert.Fail("This call must throw a NotSupportedException.");
                }
                catch (NotSupportedException) {
                    Assert.IsTrue(true);
                    if (result != null) {
                        Assert.IsFalse(result.IsUid);
                    }
                }
            }
        }

        ///<summary>
        ///  A test for Translate
        ///</summary>
        [TestMethod]
        public void TranslateFromAndToQuery()
        {
            using (var client = new ImapClient()) {
                var translator = new ImapQueryTranslator();
                var expression = client.Messages.Where(x => x.From.Contains("Foo") && x.To.Contains("Bar")).Expression;

                const string expected = "SEARCH FROM \"Foo\" TO \"Bar\"";

                var result = translator.Translate(expression);
                Assert.IsFalse(result.IsUid);
                Assert.AreEqual(expected, result.SearchCommand);
            }
        }

        ///<summary>
        ///  A test for Translate
        ///</summary>
        [TestMethod]
        public void TranslateSequenceNumberRangeQuery()
        {
            using (var client = new ImapClient()) {
                var translator = new ImapQueryTranslator();
                var expression = client.Messages.Where(x => x.SequenceNumber >= 0 && x.SequenceNumber <= 20).Expression;
                const string expected = "SEARCH 0:20";

                var result = translator.Translate(expression);
                Assert.IsFalse(result.IsUid);
                Assert.AreEqual(expected, result.SearchCommand);
            }
        }

        ///<summary>
        ///  A test for Translate
        ///</summary>
        [TestMethod]
        public void TranslateSingleSequenceNumberQuery()
        {
            using (var client = new ImapClient()) {
                var translator = new ImapQueryTranslator();
                var expression = client.Messages.Where(x => x.SequenceNumber == 5).Expression;

                const string expected = "SEARCH 5";

                var result = translator.Translate(expression);
                Assert.IsFalse(result.IsUid);
                Assert.AreEqual(expected, result.SearchCommand);
            }
        }

        ///<summary>
        ///  A test for Translate
        ///</summary>
        [TestMethod]
        public void TranslateSingleUidQuery()
        {
            using (var client = new ImapClient()) {
                var translator = new ImapQueryTranslator();
                var expression = client.Messages.Where(x => x.Uid == 5).Expression;

                const string expected = "UID SEARCH UID 5";

                var result = translator.Translate(expression);
                Assert.IsTrue(result.IsUid);
                Assert.AreEqual(expected, result.SearchCommand);
            }
        }

        ///<summary>
        ///  A test for Translate
        ///</summary>
        [TestMethod]
        public void TranslateTestSearchSeenAndAnsweredOperator()
        {
            ImapQueryTranslator target;
            Expression expression;

            using (var client = new ImapClient()) {
                target = new ImapQueryTranslator();
                expression = client.Messages.Where(x => x.Flags.HasFlag(MessageFlags.Seen) && x.Flags.HasFlag(MessageFlags.Answered)).Expression;
            }

            const string expected = "SEARCH SEEN ANSWERED";

            var result = target.Translate(expression);
            Assert.IsFalse(result.IsUid);
            Assert.AreEqual(expected, result.SearchCommand);
        }

        ///<summary>
        ///  A test for Translate
        ///</summary>
        [TestMethod]
        public void TranslateTestSearchDateSince()
        {
            ImapQueryTranslator target;
            Expression expression;

            using (var client = new ImapClient()) {
                var date = DateTime.Parse("1-Feb-1994");
                target = new ImapQueryTranslator();

                expression = client.Messages.Where(x => x.Date < date).Expression;

                // we need to evaluate the field date into a constant.
                expression = Evaluator.PartialEval(expression);
            }

            const string expected = "SEARCH SENTBEFORE 1-Feb-1994";

            var result = target.Translate(expression);
            Assert.IsFalse(result.IsUid);
            Assert.AreEqual(expected, result.SearchCommand);
        }

        ///<summary>
        ///  A test for Translate
        ///</summary>
        [TestMethod]
        public void TranslateTestSearchDateSinceSeenOrAnswered()
        {
            ImapQueryTranslator target;
            Expression expression;

            using (var client = new ImapClient()) {
                var date = DateTime.Parse("1-Feb-1994");
                target = new ImapQueryTranslator();

                expression = client.Messages.Where(x => x.Date < date && (x.Flags.HasFlag(MessageFlags.Seen) || x.Flags.HasFlag(MessageFlags.Answered))).Expression;

                // we need to evaluate the field date into a constant.
                expression = Evaluator.PartialEval(expression);
            }

            const string expected = "SEARCH SENTBEFORE 1-Feb-1994 OR SEEN ANSWERED";

            var result = target.Translate(expression);
            Assert.IsFalse(result.IsUid);
            Assert.AreEqual(expected, result.SearchCommand);
        }

        ///<summary>
        ///  A test for Translate
        ///</summary>
        [TestMethod]
        public void TranslateTestSearchSeenOperator()
        {
            try {
                using (var client = new ImapClient()) {
                    var target = new ImapQueryTranslator();
                    var expression = client.Messages.Where(x => x.Flags == MessageFlags.Seen).Expression;

                    target.Translate(expression);
                }
                Assert.Fail();
            }
            catch (NotSupportedException) {
                Assert.IsTrue(true);
            }
        }

        ///<summary>
        ///  A test for Translate
        ///</summary>
        [TestMethod]
        public void TranslateTestSearchUnseen()
        {
            ImapQueryTranslator target;
            Expression expression;

            using (var client = new ImapClient()) {
                target = new ImapQueryTranslator();
                expression = client.Messages.Where(x => !x.Flags.HasFlag(MessageFlags.Seen)).Expression;
            }

            const string expected = "SEARCH NOT SEEN";

            var result = target.Translate(expression);
            Assert.IsFalse(result.IsUid);
            Assert.AreEqual(expected, result.SearchCommand);
        }

        ///<summary>
        ///  A test for Translate
        ///</summary>
        [TestMethod]
        public void TranslateTestSearchUnseenTextKeyword()
        {
            ImapQueryTranslator target;
            Expression expression;

            using (var client = new ImapClient()) {
                target = new ImapQueryTranslator();
                expression = client.Messages.Where(x => !x.Flags.HasFlag(MessageFlags.Seen)
                                                        && x.Text.Contains("Peter File")
                                                        && x.Keywords.Contains("Cheers")
                                                        && !x.Keywords.Contains("Cheerio")).Expression;
            }

            const string expected = "SEARCH NOT SEEN TEXT \"Peter File\" KEYWORD Cheers NOT KEYWORD Cheerio";

            var result = target.Translate(expression);
            Assert.IsFalse(result.IsUid);
            Assert.AreEqual(expected, result.SearchCommand);
        }

        ///<summary>
        ///  A test for Translate
        ///</summary>
        [TestMethod]
        public void TranslateTestSearchIsOldAndNotIsNew()
        {
            ImapQueryTranslator target;
            Expression expression;

            using (var client = new ImapClient()) {
                target = new ImapQueryTranslator();
                expression = client.Messages.Where(x => x.IsNew && !x.IsOld).Expression;
            }

            const string expected = "SEARCH (RECENT UNSEEN) NOT NOT RECENT";

            var result = target.Translate(expression);
            Assert.IsFalse(result.IsUid);
            Assert.AreEqual(expected, result.SearchCommand);
        }

        ///<summary>
        ///  A test for Translate
        ///</summary>
        [TestMethod]
        public void TranslateSeperatedUidQueries()
        {
            ImapQueryTranslator target;
            Expression expression;

            using (var client = new ImapClient()) {
                target = new ImapQueryTranslator();
                expression = client.Messages.Where(x => x.Uid > 1 && x.Uid <= 555).Expression;
            }

            const string expected = "UID SEARCH UID 2:555";

            var result = target.Translate(expression);
            Assert.IsTrue(result.IsUid);
            Assert.AreEqual(expected, result.SearchCommand);
        }

        ///<summary>
        ///  A test for Translate
        ///</summary>
        [TestMethod]
        public void TranslateSeperatedUidQueriesOpenEnd()
        {
            ImapQueryTranslator target;
            Expression expression;

            using (var client = new ImapClient()) {
                target = new ImapQueryTranslator();
                expression = client.Messages.Where(x => x.Uid >= 1).Expression;
            }

            const string expected = "UID SEARCH UID 1:*";

            var resuult = target.Translate(expression);
            Assert.AreEqual(expected, resuult.SearchCommand);
        }

        ///<summary>
        ///  A test for Translate
        ///</summary>
        [TestMethod]
        public void TranslateHasHeaderField()
        {
            ImapQueryTranslator target;
            Expression expression;

            using (var client = new ImapClient()) {
                target = new ImapQueryTranslator();
                expression = client.Messages.Where(x => x.Headers.Any(y => y.Name.Contains("Foo") && y.Value.Contains("Bar"))).Expression;
            }

            const string expected = "SEARCH HEADER \"Foo\" \"Bar\"";

            var result = target.Translate(expression);
            Assert.AreEqual(expected, result.SearchCommand);
        }
    }
}