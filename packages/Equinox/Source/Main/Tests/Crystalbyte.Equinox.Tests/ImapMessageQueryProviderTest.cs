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

using System.Linq;
using Crystalbyte.Equinox.Imap;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Crystalbyte.Equinox.Security.Authentication;

namespace Crystalbyte.Equinox.Tests
{
    ///<summary>
    ///  This is a test class for ImapMessageQueryProviderTest and is intended
    ///  to contain all ImapMessageQueryProviderTest Unit Tests
    ///</summary>
    [TestClass]
    public class ImapMessageQueryProviderTest
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
        ///  A test for Execute
        ///</summary>
        [TestMethod]
        public void ExecuteQuerySubjectTest()
        {
            using (var client = Context.CreateSaslClient()) {
                var host = Context.GetHost();
                var port = ImapPorts.Ssl;
                var credential = Context.GetCredentials();

                client.Security = Security.SecurityPolicies.Explicit;
                client.Connect(host, port);
                client.Authenticate(credential, SaslMechanics.Login);
                client.Select("INBOX");

                var query = from i in client.Messages where i.SequenceNumber > 0 && i.SequenceNumber < 3 select i.Subject;

                var result = query.ToList();
                Assert.IsTrue(result.Count == 2);
            }
        }

        ///<summary>
        ///  A test for Execute
        ///</summary>
        [TestMethod]
        public void ExecuteQuerySingleEntityTest()
        {
            using (var client = Context.CreateSaslClient()) {
                var host = Context.GetHost();
                var port = ImapPorts.Ssl;
                var credential = Context.GetCredentials();

                client.Connect(host, port);
                client.Authenticate(credential, SaslMechanics.Login);
                client.Select("INBOX");

                var query = from i in client.Messages where i.SequenceNumber > 0 && i.SequenceNumber < 3 select i.Parts("1");

                var result = query.ToList();
                Assert.IsTrue(result.Count == 2);
            }
        }

        ///<summary>
        ///  A test for Execute
        ///</summary>
        [TestMethod]
        public void ExecuteQueryDateTest()
        {
            using (var client = Context.CreateSaslClient()) {
                var host = Context.GetHost();
                var port = ImapPorts.Ssl;
                var credential = Context.GetCredentials();

                client.Connect(host, port);
                client.Authenticate(credential, SaslMechanics.Login);
                client.Select("INBOX");

                var query = from i in client.Messages where i.SequenceNumber >= 1 && i.SequenceNumber <= 2 select i.Date;

                var result = query.ToList();
                Assert.IsTrue(result.Count == 2);
            }
        }

        ///<summary>
        ///  A test for Execute
        ///</summary>
        [TestMethod]
        public void ExecuteQueryEnvelopeTest()
        {
            using (var client = Context.CreateSaslClient()) {
                var host = Context.GetHost();
                var port = ImapPorts.Ssl;
                var credential = Context.GetCredentials();

                client.Connect(host, port);
                client.Authenticate(credential, SaslMechanics.Login);
                client.Select("INBOX");

                var query = from i in client.Messages where i.SequenceNumber >= 1 && i.SequenceNumber <= 2 select i.Envelope;

                var result = query.ToList();
                Assert.IsTrue(result.Count == 2);
            }
        }

        ///<summary>
        ///  A test for Execute
        ///</summary>
        [TestMethod]
        public void ExecuteQueryBodyStructureTest()
        {
            using (var client = Context.CreateSaslClient()) {
                var host = Context.GetHost();
                var port = ImapPorts.Ssl;
                var credential = Context.GetCredentials();

                client.Connect(host, port);
                client.Authenticate(credential, SaslMechanics.Login);
                client.Select("INBOX");

                var query = from i in client.Messages where i.SequenceNumber >= 1 && i.SequenceNumber <= 2 select i.BodyStructure;

                var result = query.ToList();
                Assert.IsTrue(result.Count == 2);
            }
        }

        ///<summary>
        ///  A test for Execute
        ///</summary>
        [TestMethod]
        public void ExecuteQueryMultipleValuesTest()
        {
            using (var client = Context.CreateSaslClient()) {
                var host = Context.GetHost();
                var port = ImapPorts.Ssl;
                var credential = Context.GetCredentials();

                client.Connect(host, port);
                client.Authenticate(credential, SaslMechanics.Login);
                client.Select("INBOX");

                var query = from i in client.Messages where i.SequenceNumber >= 1 && i.SequenceNumber <= 2 select new SampleContainer {Envelope = i.Envelope, MySubject = i.Subject, MyDate = i.Date, MyInternalDate = i.InternalDate};

                var result = query.ToList();
                Assert.IsTrue(result.Count == 2);
            }
        }

        ///<summary>
        ///  A test for Execute
        ///</summary>
        [TestMethod]
        public void ExecuteQueryAllHeadersUsingMemberInitTest()
        {
            using (var client = Context.CreateSaslClient()) {
                var host = Context.GetHost();
                var port = ImapPorts.Ssl;
                var credential = Context.GetCredentials();

                client.Connect(host, port);
                client.Authenticate(credential, SaslMechanics.Login);
                client.Select("INBOX");

                var query = from i in client.Messages where i.SequenceNumber >= 1 && i.SequenceNumber <= 2 select new SampleContainer {MyHeaders = i.Headers};

                var result = query.ToList();
                Assert.IsTrue(result.Count == 2);
            }
        }

        ///<summary>
        ///  A test for Execute
        ///</summary>
        [TestMethod]
        public void ExecuteQueryFlagsUsingMemberInitTest()
        {
            using (var client = Context.CreateSaslClient()) {
                var host = Context.GetHost();
                var port = ImapPorts.Ssl;
                var credential = Context.GetCredentials();

                client.Connect(host, port);
                client.Authenticate(credential, SaslMechanics.Login);
                client.Select("INBOX");

                client.Store(SequenceSet.CreateSet(1), MessageFlags.Seen, StoreProcedures.Add);
                client.Store(SequenceSet.CreateSet(2), MessageFlags.Seen, StoreProcedures.Add);

                var query = from i in client.Messages where i.SequenceNumber >= 1 && i.SequenceNumber <= 2 select new SampleContainer {MyFlags = i.Flags};

                var result = query.ToList();
                Assert.IsTrue(result.Count == 2);
                Assert.IsTrue(result[0].MyFlags.HasFlag(MessageFlags.Seen));
                Assert.IsTrue(result[1].MyFlags.HasFlag(MessageFlags.Seen));
            }
        }

        ///<summary>
        ///  A test for Execute
        ///</summary>
        [TestMethod]
        public void ExecuteQueryFromUsingMemberInitTest()
        {
            using (var client = Context.CreateSaslClient()) {
                var host = Context.GetHost();
                var port = ImapPorts.Ssl;
                var credential = Context.GetCredentials();

                client.Connect(host, port);
                client.Authenticate(credential, SaslMechanics.Login);
                client.Select("INBOX");

                var query = from i in client.Messages where i.SequenceNumber >= 1 && i.SequenceNumber <= 2 select new SampleContainer {MyContacts = i.From};

                var result = query.ToList();
                Assert.IsTrue(result.Count == 2);
                Assert.IsTrue(result[0].MyContacts.Count() > 0);
                Assert.IsTrue(result[1].MyContacts.Count() > 0);
            }
        }

        ///<summary>
        ///  A test for Execute
        ///</summary>
        [TestMethod]
        public void ExecuteTest()
        {
            using (var client = Context.CreateSaslClient()) {
                var host = Context.GetHost();
                var port = ImapPorts.Ssl;
                var credential = Context.GetCredentials();

                client.Connect(host, port);
                client.Authenticate(credential, SaslMechanics.Login);
                client.Select("INBOX");

                var query = from i in client.Messages where i.SequenceNumber >= 1 && i.SequenceNumber <= 3 select new SampleContainer {MySubject = i.Subject, MyInternalDate = i.InternalDate, MyDate = i.Date,};

                var list = query.ToList();
                Assert.IsTrue(list.Count >= 0);
            }
        }
    }
}