using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Specialized;
using System.Configuration;
using GPI.TransactionRecon.Logger;

namespace GPI.TransactionRecon.Tests
{
    [TestClass]
    public class AppConfigurationTests
    {
        private AppConfiguration _config;

        [TestInitialize]
        public void Setup()
        {
            // Simulate AppSettings
            var appSettings = new NameValueCollection
            {
                { "XMLFileFormat", ".xml" },
                { "CSVFileFormat", ".csv" },
                { "TXTFileFormat", ".txt" },
                { "EXCEL", ".xlsx" },
                { "SmtpServer", "smtp.test.com" },
                { "TRMailFrom", "from@test.com" },
                { "TRMailTo", "to@test.com" },
                { "TRMailSubject", "Test Subject" },
                { "ScheduledTime", "10:00" },
                { "TRReportLocation", "C:\\Reports" },
                { "TRLogsPath", "C:\\Logs" },
                { "TRResourcePath", "C:\\Resources" },
                { "TRReport", "ReportName" },
                { "TREODRReportMailTo", "eodrto@test.com" },
                { "TREODRReportMailFrom", "eodrfrom@test.com" },
                { "TREODRReportcountryManagers", "ManagerList" },
                { "TREODRReportSubject", "EODR Subject" },
                { "TREODRReportBody", "EODR Body" },
                { "IsTREnabled", "true" },
                { "TRIgnoreStatusList", "IgnoreList" },
                { "ParallelProcessFlag", "true" },
                { "EPD_system", "EPD" },
                { "SFTP_Systems", "System1,System2" },
                { "SFTPRetryCount", "3" },
                { "SFTPRetryDelay", "5" },
                { "SFTPSuccessMessage", "Success" },
                { "SFTPErrorMessage", "Error" },
                { "SFTPErrorEmailTo", "sftp@test.com" },
                { "IPAdd", "127.0.0.1" },
                { "TRThresholdAlertSubject", "Alert - {0} - {1}" },
                { "GpiOnlineURL", "http://gpi.test.com" },
                { "AcceptableFileFormats", ".csv,.xml" },
                { "BU_ID", "BU123" },
                { "GPI_Systems", "GPI1,GPI2" },
                { "Ignore_status_BU", "BUX" },
                { "IgnoreStatusList_Approved", "Approved" },
                { "IgnoreStatusList_Unapproved", "Unapproved" }
            };

            var secureSettings = new NameValueCollection
            {
                { "SFTPUser_TestSystem", "secureuser" },
                { "SFTPPassword_TestSystem", "securepass" }
            };

            ConfigurationManager.AppSettings.Clear();
            foreach (string key in appSettings)
            {
                ConfigurationManager.AppSettings.Set(key, appSettings[key]);
            }

            ConfigurationManager.GetSection("secureAppSettings");
            _config = new AppConfiguration();
        }

        [TestMethod]
        public void AllProperties_ShouldReturnConfiguredValues()
        {
            Assert.AreEqual(".xml", _config.XMLFileFormat);
            Assert.AreEqual(".csv", _config.CSVFileFormat);
            Assert.AreEqual(".txt", _config.TXTFileFormat);
            Assert.AreEqual(".xlsx", _config.EXCEL);
            Assert.AreEqual("smtp.test.com", _config.SmtpServer);
            Assert.AreEqual("from@test.com", _config.TRMailFrom);
            Assert.AreEqual("to@test.com", _config.TRMailTo);
            Assert.AreEqual("Test Subject", _config.TRMailSubject);
            Assert.AreEqual("10:00", _config.ScheduledTime);
            Assert.AreEqual("C:\\Reports", _config.TRReportLocation);
            Assert.AreEqual("C:\\Logs", _config.TRLogsPath);
            Assert.AreEqual("C:\\Resources", _config.TRResourcePath);
            Assert.AreEqual("ReportName", _config.TRReport);
            Assert.AreEqual("eodrto@test.com", _config.TREODRReportMailTo);
            Assert.AreEqual("eodrfrom@test.com", _config.TREODRReportMailFrom);
            Assert.AreEqual("ManagerList", _config.TREODRReportcountryManagers);
            Assert.AreEqual("EODR Subject", _config.TREODRReportSubject);
            Assert.AreEqual("EODR Body", _config.TREODRReportBody);
            Assert.AreEqual("true", _config.IsTREnabled);
            Assert.AreEqual("IgnoreList", _config.TRIgnoreStatusList);
            Assert.AreEqual("true", _config.ParallelProcessFlag);
            Assert.AreEqual("EPD", _config.EPD_system);
            Assert.AreEqual("System1,System2", _config.SFTP_Systems);
            Assert.AreEqual("3", _config.SFTPRetryCount);
            Assert.AreEqual("5", _config.SFTPRetryDelay);
            Assert.AreEqual("Success", _config.SFTPSuccessMessage);
            Assert.AreEqual("Error", _config.SFTPErrorMessage);
            Assert.AreEqual("sftp@test.com", _config.SFTPErrorEmailTo);
            Assert.AreEqual("127.0.0.1", _config.IPAdd);
            Assert.AreEqual("Alert - {0} - {1}", _config.TRThresholdAlertSubject);
            Assert.AreEqual("http://gpi.test.com", _config.GpiOnlineURL);
            Assert.AreEqual(".csv,.xml", _config.AcceptableFileFormats);
            Assert.AreEqual("BU123", _config.BU_ID);
            Assert.AreEqual("GPI1,GPI2", _config.GPI_Systems);
            Assert.AreEqual("BUX", _config.Ignore_status_BU);
            Assert.AreEqual("Approved", _config.IgnoreStatusList_Approved);
            Assert.AreEqual("Unapproved", _config.IgnoreStatusList_Unapproved);
        }

        [TestMethod]
        public void GetAppSetting_ValidKey_ReturnsValue()
        {
            var result = _config.GetAppSetting("XMLFileFormat");
            Assert.AreEqual(".xml", result);
        }

        [TestMethod]
        public void GetAppSetting_InvalidKey_ReturnsNull()
        {
            var result = _config.GetAppSetting("NonExistentKey");
            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetSftpUser_ValidKey_ReturnsSecureValue()
        {
            var result = _config.GetSftpUser("TestSystem");
            Assert.AreEqual("secureuser", result);
        }

        [TestMethod]
        public void GetSftpUser_InvalidKey_ReturnsEmpty()
        {
            var result = _config.GetSftpUser("UnknownSystem");
            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void GetSftpPassword_ValidKey_ReturnsSecureValue()
        {
            var result = _config.GetSftpPassword("TestSystem");
            Assert.AreEqual("securepass", result);
        }

        [TestMethod]
        public void GetSftpPassword_InvalidKey_ReturnsEmpty()
        {
            var result = _config.GetSftpPassword("UnknownSystem");
            Assert.AreEqual(string.Empty, result);
        }
    }
}
