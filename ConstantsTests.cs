using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GPI.TransactionRecon.Logger;

namespace GPI.TransactionRecon.Tests
{
    [TestClass]
    public class AppConfigurationTests
    {
        private AppConfiguration _config;

        [TestInitialize]
        public void TestInitialize()
        {
            // Instantiate the class under test
            _config = new AppConfiguration();
        }

        [TestMethod]
        public void All_AppSettings_Properties_Should_Map_To_ConfigValues()
        {
            // Find all public string properties declared in AppConfiguration (excluding DatabaseConnectionString)
            var props = typeof(AppConfiguration)
                        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(p => p.PropertyType == typeof(string) 
                                 && p.Name != nameof(AppConfiguration.DatabaseConnectionString));

            foreach (var prop in props)
            {
                // Expected value from App.config
                string expected = ConfigurationManager.AppSettings[prop.Name];
                
                // Actual from property getter
                string actual = (string)prop.GetValue(_config);
                
                Assert.AreEqual(
                    expected,
                    actual,
                    $"Property '{prop.Name}' did not return the expected value."
                );
            }
        }

        [TestMethod]
        public void DatabaseConnectionString_Should_Return_QADB_ConnectionString()
        {
            var connString = _config.DatabaseConnectionString;
            Assert.IsNotNull(connString, "DatabaseConnectionString should not be null.");
            Assert.IsTrue(
                connString.Contains("Initial Catalog=TestDB"),
                "Connection string did not contain the expected database name."
            );
        }

        [TestMethod]
        public void GetAppSetting_Should_Return_SpecificValue()
        {
            // Pick one known key
            string expected = ConfigurationManager.AppSettings["TRMailSubject"];
            string actual = _config.GetAppSetting("TRMailSubject");
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void GetSftpUser_Should_Return_Value_When_Key_Present()
        {
            string actual = _config.GetSftpUser("TestSystem");
            Assert.AreEqual("TestUser", actual);
        }

        [TestMethod]
        public void GetSftpPassword_Should_Return_Value_When_Key_Present()
        {
            string actual = _config.GetSftpPassword("TestSystem");
            Assert.AreEqual("TestPass", actual);
        }

        [TestMethod]
        public void GetSftpUser_Should_Return_Empty_When_Key_Missing()
        {
            string actual = _config.GetSftpUser("UnknownSystem");
            Assert.AreEqual(string.Empty, actual);
        }

        [TestMethod]
        public void GetSftpPassword_Should_Return_Empty_When_Key_Missing()
        {
            string actual = _config.GetSftpPassword("UnknownSystem");
            Assert.AreEqual(string.Empty, actual);
        }
    }
}
