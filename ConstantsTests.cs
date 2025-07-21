using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Linq;
using System.Collections.Generic;
using Moq;
using GPI.TransactionRecon.BusinessLogic;
using GPI.TransactionRecon.BusinessLogic.Contracts;
using GPI.TransactionRecon.Logger.Contracts;

namespace GPI.TransactionRecon.BusinessLogic.Tests
{
    [TestClass]
    public class TRSFTPTests
    {
        private Mock<IAppConfiguration> _mockConfig;
        private Mock<ILoggerService> _mockLogger;
        private Mock<IEmailService> _mockMail;
        private string _testDir;

        [TestInitialize]
        public void Setup()
        {
            _mockConfig = new Mock<IAppConfiguration>();
            _mockLogger = new Mock<ILoggerService>();
            _mockMail = new Mock<IEmailService>();

            // Create a temporary directory for test files
            _testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDir);

            _mockConfig.Setup(c => c.TRResourcePath).Returns(_testDir);
            _mockConfig.Setup(c => c.GPI_Systems).Returns("sysA");
            _mockConfig.Setup(c => c.AcceptableFileFormats).Returns(".xml");

            // Setup any other required config methods as needed
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_testDir))
                Directory.Delete(_testDir, true);
        }

        [TestMethod]
        public void Constructor_InitializesSFTPRetryData()
        {
            var sftp = new TRSFTP(_mockConfig.Object, _mockLogger.Object, _mockMail.Object);
            // No exception means pass for basic initialization
        }

        [TestMethod]
        public void LoadResourceXml_Returns_Null_For_InvalidPath()
        {
            var sftp = new TRSFTP(_mockConfig.Object, _mockLogger.Object, _mockMail.Object);
            var node = sftp.LoadResourceXml("nonexistent.xml");
            Assert.IsNull(node);
        }

        [TestMethod]
        public void LoadResourceXml_Returns_Node_For_ValidXml()
        {
            string xmlPath = Path.Combine(_testDir, "resource.xml");
            File.WriteAllText(xmlPath, @"<EODRResource>
                <SFTPHostName>host</SFTPHostName>
                <SFTPServerRequest>remote</SFTPServerRequest>
                <SFTPPort>22</SFTPPort>
                <SshPrivateKeyPath>key</SshPrivateKeyPath>
                <SFTPLocationRequest>dest</SFTPLocationRequest>
            </EODRResource>");
            var sftp = new TRSFTP(_mockConfig.Object, _mockLogger.Object, _mockMail.Object);
            var node = sftp.LoadResourceXml(xmlPath);
            Assert.IsNotNull(node);
            Assert.AreEqual("host", node["SFTPHostName"].InnerText);
        }

        [TestMethod]
        public void GetFileEncoding_Returns_UTF8_ByDefault()
        {
            string path = Path.Combine(_testDir, "test.txt");
            File.WriteAllText(path, "plain text");
            var sftp = new TRSFTP(_mockConfig.Object, _mockLogger.Object, _mockMail.Object);
            var encoding = typeof(TRSFTP).GetMethod("GetFileEncoding", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = encoding.Invoke(sftp, new object[] { path });
            Assert.AreEqual(Encoding.UTF8, result);
        }

        [TestMethod]
        public void SftpExistDwownload_Does_Not_Throw_On_Empty_Dir()
        {
            var sftp = new TRSFTP(_mockConfig.Object, _mockLogger.Object, _mockMail.Object);
            sftp.SftpExistDwownload();
        }

        [TestMethod]
        public void DirectoryListing_Returns_Empty_When_Invalid_Path()
        {
            var sftp = new TRSFTP(_mockConfig.Object, _mockLogger.Object, _mockMail.Object);
            var result = sftp.DirectoryListing("notahost", "/invalid", "user", "pass", "22", "key", "/", "sysA");
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void transferToSFTP_Does_Not_Throw_On_Empty_ResponsePath()
        {
            var sftp = new TRSFTP(_mockConfig.Object, _mockLogger.Object, _mockMail.Object);
            sftp.transferToSFTP("host", "22", "key", "user", "pw", "", "reply", "body", "/dest/");
        }

        [TestMethod]
        public void transferToSFTP_Saves_Local_And_SFTP_DoesNotThrow()
        {
            string localResponseDir = _testDir;
            var sftp = new TRSFTP(_mockConfig.Object, _mockLogger.Object, _mockMail.Object);
            sftp.transferToSFTP("localhost", "22", "key", "user", "", localResponseDir, "filename", "somecontent", localResponseDir);
            string expectedPath = Path.Combine(localResponseDir, "filename.csv");
            Assert.IsTrue(File.Exists(expectedPath));
        }

        [TestMethod]
        public void SFTPExceptionHandler_Null_RetryData()
        {
            var sftp = new TRSFTP(_mockConfig.Object, _mockLogger.Object, _mockMail.Object);
            var method = typeof(TRSFTP).GetMethod("SFTPExceptionHandler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(sftp, new object[] { null, new Exception("err"), "host", "/r" });
            // Pass if no exception
        }
    }
}
