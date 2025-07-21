using System;
using System.Configuration;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GPI.TransactionRecon.Logger;

namespace GPI.TransactionRecon.Tests
{
    [TestClass]
    public class AppConfigurationTests
    {
        private AppConfiguration _config;

        [TestInitialize]
        public void Arrange()
        {
            _config = new AppConfiguration();
        }

        // 1) Individual property tests

        [TestMethod]
        public void XMLFileFormat_WhenKeyExists_ReturnsExpectedValue()
        {
            var expected = ConfigurationManager.AppSettings["XMLFileFormat"];
            var actual   = _config.XMLFileFormat;
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void DatabaseConnectionString_WhenExists_ReturnsQADBConnectionString()
        {
            var expected = ConfigurationManager.ConnectionStrings["QADB"].ConnectionString;
            var actual   = _config.DatabaseConnectionString;
            StringAssert.Contains(actual, expected);
        }

        // 2) Data-driven for the rest of simple string properties

        [DataTestMethod]
        [DataRow("CSVFileFormat")]
        [DataRow("TXTFileFormat")]
        [DataRow("EXCEL")]
        [DataRow("SmtpServer")]
        [DataRow("TRMailFrom")]
        [DataRow("TRMailTo")]
        [DataRow("TRMailSubject")]
        [DataRow("ScheduledTime")]
        [DataRow("TRReportLocation")]
        [DataRow("TRLogsPath")]
        [DataRow("TRResourcePath")]
        [DataRow("TRReport")]
        [DataRow("TREODRReportMailTo")]
        [DataRow("TREODRReportMailFrom")]
        [DataRow("TREODRReportcountryManagers")]
        [DataRow("TREODRReportSubject")]
        [DataRow("TREODRReportBody")]
        [DataRow("IsTREnabled")]
        [DataRow("TRIgnoreStatusList")]
        [DataRow("ParallelProcessFlag")]
        [DataRow("EPD_system")]
        [DataRow("SFTP_Systems")]
        [DataRow("SFTPRetryCount")]
        [DataRow("SFTPRetryDelay")]
        [DataRow("SFTPSuccessMessage")]
        [DataRow("SFTPErrorMessage")]
        [DataRow("SFTPErrorEmailTo")]
        [DataRow("IPAdd")]
        [DataRow("TRThresholdAlertSubject")]
        [DataRow("GpiOnlineURL")]
        [DataRow("AcceptableFileFormats")]
        [DataRow("BU_ID")]
        [DataRow("GPI_Systems")]
        [DataRow("Ignore_status_BU")]
        [DataRow("IgnoreStatusList_Approved")]
        [DataRow("IgnoreStatusList_Unapproved")]
        public void Property_WhenKeyExists_ReturnsAppSetting(string key)
        {
            var prop     = typeof(AppConfiguration).GetProperty(key);
            var expected = ConfigurationManager.AppSettings[key];
            var actual   = (string)prop.GetValue(_config);
            Assert.AreEqual(expected, actual, $"{key} mismatch");
        }

        // 3) GetAppSetting helper

        [TestMethod]
        public void GetAppSetting_WhenExists_ReturnsValue()
        {
            var expected = ConfigurationManager.AppSettings["TRMailSubject"];
            var actual   = _config.GetAppSetting("TRMailSubject");
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void GetAppSetting_WhenMissing_ReturnsNull()
        {
            var actual = _config.GetAppSetting("NoSuchKey");
            Assert.IsNull(actual);
        }

        // 4) secureAppSettings helpers

        [TestMethod]
        public void GetSftpUser_WhenPresent_ReturnsUser()
        {
            var actual = _config.GetSftpUser("TestSystem");
            Assert.AreEqual("TestUser", actual);
        }

        [TestMethod]
        public void GetSftpUser_WhenMissing_ReturnsEmpty()
        {
            var actual = _config.GetSftpUser("Unknown");
            Assert.AreEqual(string.Empty, actual);
        }

        [TestMethod]
        public void GetSftpPassword_WhenPresent_ReturnsPassword()
        {
            var actual = _config.GetSftpPassword("TestSystem");
            Assert.AreEqual("TestPass", actual);
        }

        [TestMethod]
        public void GetSftpPassword_WhenMissing_ReturnsEmpty()
        {
            var actual = _config.GetSftpPassword("Unknown");
            Assert.AreEqual(string.Empty, actual);
        }
    }
}
