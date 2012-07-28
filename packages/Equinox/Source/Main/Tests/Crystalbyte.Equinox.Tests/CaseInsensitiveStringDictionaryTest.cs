using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using Crystalbyte.Equinox.Mime.Collections;

namespace Crystalbyte.Equinox.Tests
{
    
    
    /// <summary>
    ///This is a test class for CaseInsensitiveStringDictionaryTest and is intended
    ///to contain all CaseInsensitiveStringDictionaryTest Unit Tests
    ///</summary>
    [TestClass()]
    public class CaseInsensitiveStringDictionaryTest
    {
        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

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


        /// <summary>
        ///A test for Add
        ///</summary>
        [TestMethod()]
        public void AddTest()
        {
            CaseInsensitiveStringDictionary target = new CaseInsensitiveStringDictionary();
            string key = "kEybA2"; 
            string value = "value"; 
            target.Add(key, value);
            Assert.IsTrue(target.Count > 0);
        }

        /// <summary>
        ///A test for ContainsKey
        ///</summary>
        [TestMethod()]
        public void ContainsKeyTest()
        {
            CaseInsensitiveStringDictionary target = new CaseInsensitiveStringDictionary();
            var key = "UIEasdh&/%%2222";
            target.Add(key, "foo");
            var expected = true;
            var actual = target.ContainsKey(key) 
                && target.ContainsKey(key.ToLower()) 
                && target.ContainsKey(key.ToUpper());
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        ///A test for Remove
        ///</summary>
        [TestMethod()]
        public void RemoveTest()
        {
            CaseInsensitiveStringDictionary target = new CaseInsensitiveStringDictionary(); 
            var key = "UIEasdh&/%%2222";
            target.Add(key, "foo");
            Assert.IsTrue(target.Count > 0);
            target.Remove(key.ToUpper());
            Assert.IsTrue(target.Count == 0);
        }

        /// <summary>
        ///A test for Remove
        ///</summary>
        [TestMethod()]
        public void IndexerTest()
        {
            CaseInsensitiveStringDictionary target = new CaseInsensitiveStringDictionary();
            var key = "UIEasdh&/%%2222";
            var value = "foo";
            target.Add(key, value);
            Assert.IsTrue(target.Count > 0);
            Assert.IsTrue(target[key.ToUpper()] == value);
        }
    }
}
