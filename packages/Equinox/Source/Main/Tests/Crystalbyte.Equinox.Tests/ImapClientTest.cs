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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Crystalbyte.Equinox.Imap;
using Crystalbyte.Equinox.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Crystalbyte.Equinox.Security.Authentication;

namespace Crystalbyte.Equinox.Tests
{
    ///<summary>
    ///  This is a test class for SaslClientTest and is intended
    ///  to contain all SaslClientTest Unit Tests
    ///</summary>
    [TestClass]
    public class ImapClientTest
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
        ///  A test for Connect
        ///</summary>
        [TestMethod]
        public void ConnectImplicitTest()
        {
            var client = Context.CreateSaslClient();
            var host = Context.GetHost();
            var port = ImapPorts.Ssl;
            client.Connect(host, port);
            Assert.IsTrue(client.IsConnected);
            client.Disconnect();
            Assert.IsFalse(client.IsConnected);
        }

        ///<summary>
        ///  A test for Connect
        ///</summary>
        [TestMethod]
        public void ConnectExplicitTest()
        {
            var client = Context.CreateSaslClient();
            client.Security = SecurityPolicies.Explicit;
            var host = Context.GetHost();
            var port = ImapPorts.Ssl;
            client.Connect(host, port);
            Assert.IsTrue(client.IsConnected);
            client.Disconnect();
            Assert.IsFalse(client.IsConnected);
        }

        ///<summary>
        ///  A test for Connect
        ///</summary>
        [TestMethod]
        public void AuthenticateWithValidCredentials()
        {
            using (var client = Context.CreateSaslClient()) {
                client.Security = SecurityPolicies.Explicit;
                var host = Context.GetHost();
                var port = ImapPorts.Ssl;

                client.Connect(host, port);
                Assert.IsTrue(client.IsConnected);

                var credentials = Context.GetCredentials();
             client.Authenticate(credentials, SaslMechanics.Login);
                Assert.IsTrue(client.IsAuthenticated);
            }
        }

        /// <summary>
        ///   Testing the clients append method.
        /// </summary>
        [TestMethod]
        public void AppendTest()
        {
            using (var client = Context.CreateSaslClient()) {
                client.Security = SecurityPolicies.Explicit;
                var host = Context.GetHost();
                var port = ImapPorts.Ssl;

                client.Connect(host, port);
                var credentials = Context.GetCredentials();
             client.Authenticate(credentials, SaslMechanics.Login);

                var message = Context.GetTestMessage();
                var response = client.Append("INBOX", message, MessageFlags.Flagged);
                Assert.IsTrue(response.IsOk);
            }
        }

        ///<summary>
        ///  A test for Connect
        ///</summary>
        //[TestMethod]
        public void AuthenticateWithValidCredentialsCramMd5()
        {
            using (var client = Context.CreateSaslClient()) {
                Assert.Inconclusive("Test server does not support cram md5.");
                client.Security = SecurityPolicies.Explicit;
                var host = Context.GetHost();
                var port = ImapPorts.Ssl;

                client.Connect(host, port);
                Assert.IsTrue(client.IsConnected);

                var credentials = Context.GetCredentials();
             client.Authenticate(credentials, SaslMechanics.Login);
                Assert.IsTrue(client.IsAuthenticated);
            }
        }

        ///<summary>
        ///  A test for Connect
        ///</summary>
        [TestMethod]
        public void Expunge()
        {
            using (var client = Context.CreateSaslClient()) {
                client.Security = SecurityPolicies.Explicit;
                var host = Context.GetHost();
                var port = ImapPorts.Ssl;

                client.Connect(host, port);
                var credentials = Context.GetCredentials();
             client.Authenticate(credentials, SaslMechanics.Login);
                client.Select("INBOX");
                client.Store(SequenceSet.CreateSet(1), MessageFlags.Deleted, StoreProcedures.Add);
                client.StatusUpdateReceived += (sender, e) => Debug.WriteLine("Status update received.");
                var response = client.Expunge();
                Assert.IsTrue(response.IsOk);
            }
        }


        ///<summary>
        ///  A test for Connect
        ///</summary>
        [TestMethod]
        public void AuthenticateWithValidCredentialsPlain()
        {
            using (var client = Context.CreateSaslClient()) {
                client.Security = SecurityPolicies.Explicit;
                var host = Context.GetHost();
                var port = ImapPorts.Ssl;

                client.Connect(host, port);
                Assert.IsTrue(client.IsConnected);

                var credentials = Context.GetCredentials();
             client.Authenticate(credentials, SaslMechanics.Login);
                Assert.IsTrue(client.IsAuthenticated);
            }
        }

        ///<summary>
        ///  A test for Connect
        ///</summary>
        //[TestMethod]
        public void AuthenticateWithValidCredentialsAuthenticate()
        {
            Assert.Inconclusive("no server to test yet.");
            using (var client = Context.CreateSaslClient()) {
                client.Security = SecurityPolicies.Explicit;
                var host = Context.GetHost();
                var port = ImapPorts.Ssl;

                client.Connect(host, port);
                Assert.IsTrue(client.IsConnected);

                var credentials = Context.GetCredentials();
             client.Authenticate(credentials, SaslMechanics.Login);
                Assert.IsTrue(client.IsAuthenticated);

                client.Disconnect();
                Assert.IsFalse(client.IsConnected);
            }
        }

        ///<summary>
        ///  A test for invalid credentials
        ///</summary>
        [TestMethod]
        public void AuthenticateWithInvalidCredentials()
        {
            using (var client = Context.CreateSaslClient()) {
                var host = Context.GetHost();
                var port = ImapPorts.Ssl;

                client.Connect(host, port);
                Assert.IsTrue(client.IsConnected);

                Assert.IsNotNull(client.ServerCapability);
                var credentials = new NetworkCredential("bla", "foo");

                var success = client.Authenticate(credentials);
                Assert.IsFalse(success);
            }
        }

        ///<summary>
        ///  A test for invalid credentials
        ///</summary>
        [TestMethod]
        public void AuthenticateWithClientCertificate()
        {
            using (var client = Context.CreateSaslClient()) {
                var host = Context.GetHost();
                var port = ImapPorts.Ssl;

                client.Certificates.Add(new X509Certificate2("../../../Tests/Crystalbyte.Equinox.Tests/Certificates/equinox.cer"));
                client.Connect(host, port);
                Assert.IsTrue(client.IsConnected);
            }
        }

        ///<summary>
        ///  A test for examine
        ///</summary>
        [TestMethod]
        public void Examine()
        {
            using (var client = Context.CreateSaslClient()) {
                var host = Context.GetHost();
                var port = ImapPorts.Ssl;
                client.Connect(host, port);
                var credentials = Context.GetCredentials();
             client.Authenticate(credentials, SaslMechanics.Login);
                var response = client.Examine("INBOX");
                Assert.IsTrue(response.MailboxInfo.UidValidity != 0);
                Assert.IsTrue(response.MailboxInfo.Name == "INBOX");
            }
        }

        ///<summary>
        ///  A test for examine
        ///</summary>
        [TestMethod]
        public void Store()
        {
            using (var client = Context.CreateSaslClient()) {
                var host = Context.GetHost();
                var port = ImapPorts.Ssl;

                client.Connect(host, port);
                var credentials = Context.GetCredentials();
             client.Authenticate(credentials, SaslMechanics.Login);

                client.Select("INBOX");

                var set = SequenceSet.CreateSet(1, 1);
                client.Store(set, MessageFlags.Seen | MessageFlags.Flagged, StoreProcedures.Add);
            }
        }

        ///<summary>
        ///  A test for search
        ///</summary>
        [TestMethod]
        public void Search()
        {
            using (var client = Context.CreateSaslClient()) {
                var host = Context.GetHost();
                var port = ImapPorts.Ssl;

                client.Connect(host, port);
                var credentials = Context.GetCredentials();
             client.Authenticate(credentials, SaslMechanics.Login);

                client.Select("INBOX");

                var response = client.Search("OLD");
                Assert.IsTrue(response.SequenceSet.Values.Count > 0);
            }
        }

        ///<summary>
        ///  A test for search
        ///</summary>
        [TestMethod]
        public void Fetch()
        {
            using (var client = Context.CreateSaslClient()) {
                var host = Context.GetHost();
                var port = ImapPorts.Ssl;

                client.Connect(host, port);
                var credentials = Context.GetCredentials();
             client.Authenticate(credentials, SaslMechanics.Login);

                client.Select("INBOX");

                var set = SequenceSet.CreateSet(1);
                var response = client.Fetch(set, "FLAGS");
                Assert.IsTrue(response.IsOk);
                Assert.IsTrue(response.Text.Contains("FLAGS"));
            }
        }

        ///<summary>
        ///  A test for search
        ///</summary>
        [TestMethod]
        public void FetchMessageByUid()
        {
            using (var client = Context.CreateSaslClient()) {
                var host = Context.GetHost();
                var port = ImapPorts.Ssl;

                client.Connect(host, port);
                var credentials = Context.GetCredentials();
             client.Authenticate(credentials, SaslMechanics.Login);

                client.Select("INBOX");

                var uid = client.Messages.Where(x => x.SequenceNumber == 1).Select(x => x.Uid).ToList().First();
                var message = client.FetchMessageByUid(uid);
                Assert.IsTrue(!string.IsNullOrEmpty(message.Subject));
                Assert.IsNotNull(message);
            }
        }

        ///<summary>
        ///  A test for search
        ///</summary>
        [TestMethod]
        public void FetchAttachmentByToken()
        {
            using (var client = Context.CreateSaslClient()) {
                var host = Context.GetHost();
                var port = ImapPorts.Ssl;

                client.Connect(host, port);
                var credentials = Context.GetCredentials();
             client.Authenticate(credentials, SaslMechanics.Login);

                client.Select("INBOX");

                var query = from i in client.Messages where i.SequenceNumber >= 1 && i.SequenceNumber <= 100 select i.BodyStructure;

                var list = query.ToList();
                foreach (var info in list) {
                    if (info.Attachments.Count() > 0) {
                        var ai = info.Attachments.First();
                        var attachment = client.FetchAttachment(ai);
                        Assert.IsTrue(attachment.Filename != null);
                        return;
                    }
                }
            }
        }

        ///<summary>
        ///  A test for search
        ///</summary>
        [TestMethod]
        public void FetchViewByToken()
        {
            using (var client = Context.CreateSaslClient()) {
                var host = Context.GetHost();
                var port = ImapPorts.Ssl;

                client.Connect(host, port);
                var credentials = Context.GetCredentials();
             client.Authenticate(credentials, SaslMechanics.Login);

                client.Select("INBOX");

                var query = from i in client.Messages where i.SequenceNumber >= 1 && i.SequenceNumber <= 100 select i.BodyStructure;

                var list = query.ToList();
                foreach (var info in list) {
                    if (info.Views.Count() > 0) {
                        var ai = info.Views.First();
                        var view = client.FetchView(ai);
                        Assert.IsTrue(view.MediaType != null);
                        return;
                    }
                }
            }
        }

        ///<summary>
        ///  A test for search
        ///</summary>
        [TestMethod]
        public void FetchNestedMessageByToken()
        {
            using (var client = Context.CreateSaslClient()) {
                var host = Context.GetHost();
                var port = ImapPorts.Ssl;

                client.Connect(host, port);
                var credentials = Context.GetCredentials();
             client.Authenticate(credentials, SaslMechanics.Login);

                client.Select("INBOX");

                var query = from i in client.Messages where i.SequenceNumber >= 1 && i.SequenceNumber <= 100 select i.BodyStructure;

                var list = query.ToList();
                foreach (var info in list) {
                    if (info.NestedMessages.Count() > 0) {
                        var ai = info.NestedMessages.First();
                        var nested = client.FetchNestedMessage(ai);
                        Assert.IsTrue(nested.To.Count > 0);
                        return;
                    }
                }
            }
        }


        ///<summary>
        ///  A test for search
        ///</summary>
        [TestMethod]
        public void FetchBodyStructure()
        {
            using (var client = Context.CreateSaslClient()) {
                var host = Context.GetHost();
                var port = ImapPorts.Ssl;

                client.Connect(host, port);
                var credentials = Context.GetCredentials();
             client.Authenticate(credentials, SaslMechanics.Login);

                client.Select("INBOX");

                var query = from i in client.Messages where i.SequenceNumber >= 1 && i.SequenceNumber <= 1000 select i.BodyStructure;
                var structs = query.ToList();
                using (var sw = new StringWriter()) {
                    foreach (var @struct in structs) {
                        if (@struct == null) {
                            continue;
                        }
                        var normalized = @struct.OriginalString.Substring(0, @struct.OriginalString.Length - 1 - @struct.SequenceNumber.ToString().Length);
                        sw.WriteLine(normalized);
                    }

                    sw.ToString();
                }

                Assert.IsTrue(structs.Count > 0);
            }
        }

        ///<summary>
        ///  A test for search
        ///</summary>
        [TestMethod]
        public void List()
        {
            using (var client = Context.CreateSaslClient()) {
                var host = Context.GetHost();
                var port = ImapPorts.Ssl;

                client.Connect(host, port);
                var credentials = Context.GetCredentials();
             client.Authenticate(credentials, SaslMechanics.Login);

                client.Select("INBOX");

                var response = client.List(string.Empty, "*");
                Assert.IsTrue(response.IsOk);
                Assert.IsTrue(response.Mailboxes.Count() > 0);
            }
        }

        ///<summary>
        ///  A test for Noop
        ///</summary>
        [TestMethod]
        public void Noop()
        {
            using (var client = Context.CreateSaslClient()) {
                var host = Context.GetHost();
                var port = ImapPorts.Ssl;

                client.Connect(host, port);
                var credentials = Context.GetCredentials();
             client.Authenticate(credentials, SaslMechanics.Login);
                try {
                    client.Noop();
                }
                catch (Exception) {
                    Assert.Fail("Should never go wrong.");
                }
            }
        }

        //[TestMethod]
        public void IdleTest()
        {
            using (var client = Context.CreateSaslClient()) {
                var host = Context.GetHost();
                var port = ImapPorts.Ssl;

                client.Connect(host, port);
                var credentials = Context.GetCredentials();
             client.Authenticate(credentials, SaslMechanics.Login);
                client.Select("INBOX");
                client.StatusUpdateReceived += (sender, e) => Debug.WriteLine("Status update received.");

                client.StartIdle();
            }
        }
    }
}