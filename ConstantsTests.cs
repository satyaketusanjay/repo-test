using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            _config = new AppConfiguration();
        }

        [TestMethod]
        public void All_AppSettings_Properties_ShouldNotBeNullOrEmpty()
        {
            Assert.IsFalse(string.IsNullOrEmpty(_config.XMLFileFormat));
            Assert.IsFalse(string.IsNullOrEmpty(_config.CSVFileFormat));
            Assert.IsFalse(string.IsNullOrEmpty(_config.TXTFileFormat));
            Assert.IsFalse(string.IsNullOrEmpty(_config.EXCEL));
            Assert.IsFalse(string.IsNullOrEmpty(_config.SmtpServer));
            Assert.IsFalse(string.IsNullOrEmpty(_config.TRMailFrom));
            Assert.IsFalse(string.IsNullOrEmpty(_config.TRMailTo));
            Assert.IsFalse(string.IsNullOrEmpty(_config.TRMailSubject));
            Assert.IsFalse(string.IsNullOrEmpty(_config.ScheduledTime));
            Assert.IsFalse(string.IsNullOrEmpty(_config.TRReportLocation));
            Assert.IsFalse(string.IsNullOrEmpty(_config.TRLogsPath));
            Assert.IsFalse(string.IsNullOrEmpty(_config.TRResourcePath));
            Assert.IsFalse(string.IsNullOrEmpty(_config.TRReport));
            Assert.IsFalse(string.IsNullOrEmpty(_config.TREODRReportMailTo));
            Assert.IsFalse(string.IsNullOrEmpty(_config.TREODRReportMailFrom));
            Assert.IsFalse(string.IsNullOrEmpty(_config.TREODRReportcountryManagers));
            Assert.IsFalse(string.IsNullOrEmpty(_config.TREODRReportSubject));
            Assert.IsFalse(string.IsNullOrEmpty(_config.TREODRReportBody));
            Assert.IsFalse(string.IsNullOrEmpty(_config.IsTREnabled));
            Assert.IsFalse(string.IsNullOrEmpty(_config.TRIgnoreStatusList));
            Assert.IsFalse(string.IsNullOrEmpty(_config.ParallelProcessFlag));
            Assert.IsFalse(string.IsNullOrEmpty(_config.EPD_system));
            Assert.IsFalse(string.IsNullOrEmpty(_config.SFTP_Systems));
            Assert.IsFalse(string.IsNullOrEmpty(_config.SFTPRetryCount));
            Assert.IsFalse(string.IsNullOrEmpty(_config.SFTPRetryDelay));
            Assert.IsFalse(string.IsNullOrEmpty(_config.SFTPSuccessMessage));
            Assert.IsFalse(string.IsNullOrEmpty(_config.SFTPErrorMessage));
            Assert.IsFalse(string.IsNullOrEmpty(_config.SFTPErrorEmailTo));
            Assert.IsFalse(string.IsNullOrEmpty(_config.IPAdd));
            Assert.IsFalse(string.IsNullOrEmpty(_config.TRThresholdAlertSubject));
            Assert.IsFalse(string.IsNullOrEmpty(_config.GpiOnlineURL));
            Assert.IsFalse(string.IsNullOrEmpty(_config.AcceptableFileFormats));
            Assert.IsFalse(string.IsNullOrEmpty(_config.BU_ID));
            Assert.IsFalse(string.IsNullOrEmpty(_config.GPI_Systems));
            Assert.IsFalse(string.IsNullOrEmpty(_config.Ignore_status_BU));
            Assert.IsFalse(string.IsNullOrEmpty(_config.IgnoreStatusList_Approved));
            Assert.IsFalse(string.IsNullOrEmpty(_config.IgnoreStatusList_Unapproved));
            Assert.IsFalse(string.IsNullOrEmpty(_config.DatabaseConnectionString));
        }

        [TestMethod]
        public void GetAppSetting_ValidKey_ShouldReturnValue()
        {
            var value = _config.GetAppSetting("XMLFileFormat");
            Assert.AreEqual("*.xml", value);
        }

        [TestMethod]
        public void GetAppSetting_InvalidKey_ShouldReturnNull()
        {
            var value = _config.GetAppSetting("NonExistentKey");
            Assert.IsNull(value);
        }

        [TestMethod]
        public void GetSftpUser_ValidKey_ShouldReturnValue()
        {
            var value = _config.GetSftpUser("ABA");
            Assert.AreEqual("svc_latam_gpi", value);
        }

        [TestMethod]
        public void GetSftpUser_InvalidKey_ShouldReturnEmpty()
        {
            var value = _config.GetSftpUser("UNKNOWN");
            Assert.AreEqual(string.Empty, value);
        }

        [TestMethod]
        public void GetSftpPassword_ValidKey_ShouldReturnValue()
        {
            var value = _config.GetSftpPassword("ABA");
            Assert.AreEqual(string.Empty, value); // empty string in config
        }

        [TestMethod]
        public void GetSftpPassword_InvalidKey_ShouldReturnEmpty()
        {
            var value = _config.GetSftpPassword("UNKNOWN");
            Assert.AreEqual(string.Empty, value);
        }
    }
}
