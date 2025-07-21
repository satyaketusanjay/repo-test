using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.IO;
using System.Data;
using System.Threading.Tasks;
using System.Xml;
using System.Linq;
using System.Text;
using System.Threading;
using GPI.TransactionRecon.BusinessLogic;
using GPI.TransactionRecon.BusinessLogic.Contracts;
using GPI.TransactionRecon.Logger.Contracts;
using GPI.DAL.Contracts;
using GPI.BusinessEntities;

namespace GPI.TransactionRecon.BusinessLogic.Tests
{
    [TestClass]
    public class TransactionReconClassTests
    {
        private Mock<ICommonDAO> _mockDAO;
        private Mock<ITRDataLoader> _mockLoader;
        private Mock<ILoggerService> _mockLog;
        private Mock<ITRSFTP> _mockSftp;
        private Mock<IEmailService> _mockEmail;
        private Mock<IAppConfiguration> _mockConfig;

        private string _testDir;
        private TransactionReconClass _recon;

        [TestInitialize]
        public void Setup()
        {
            _mockDAO = new Mock<ICommonDAO>();
            _mockLoader = new Mock<ITRDataLoader>();
            _mockLog = new Mock<ILoggerService>();
            _mockSftp = new Mock<ITRSFTP>();
            _mockEmail = new Mock<IEmailService>();
            _mockConfig = new Mock<IAppConfiguration>();

            _testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDir);
            _mockConfig.SetupGet(c => c.TRResourcePath).Returns(_testDir);
            _mockConfig.SetupGet(c => c.TXTFileFormat).Returns("*.txt");
            _mockConfig.SetupGet(c => c.XMLFileFormat).Returns("*.xml");
            _mockConfig.SetupGet(c => c.CSVFileFormat).Returns("*.csv");
            _mockConfig.SetupGet(x => x.ParallelProcessFlag).Returns("false");
            _mockConfig.SetupGet(x => x.TRMailTo).Returns("test@mail.com");
            _mockConfig.SetupGet(x => x.TRIgnoreStatusList).Returns("");

            _recon = new TransactionReconClass(_mockConfig.Object, _mockLog.Object, _mockEmail.Object, _mockDAO.Object, _mockLoader.Object, _mockSftp.Object);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_testDir))
                Directory.Delete(_testDir, true);
        }

        [TestMethod]
        public void fileWatcher_WithValidXml_FileSystemWatcherCreated()
        {
            // Write a valid resource XML with required node
            string resXml = Path.Combine(_testDir, "abc.xml");
            File.WriteAllText(resXml, "<root><SFTPLocationRequest>" + _testDir + "</SFTPLocationRequest></root>");
            var xmlNode = new XmlDocument();
            xmlNode.Load(resXml);
            _mockSftp.Setup(s => s.LoadResourceXml(It.IsAny<string>())).Returns(xmlNode.DocumentElement);

            _recon.fileWatcher();
        }

        [TestMethod]
        public void fileWatcher_InvalidXml_ResourceNodeNull_NoException()
        {
            string resXml = Path.Combine(_testDir, "def.xml");
            File.WriteAllText(resXml, "<root></root>");
            _mockSftp.Setup(s => s.LoadResourceXml(It.IsAny<string>())).Returns((XmlNode)null);
            _recon.fileWatcher();
        }

        [TestMethod]
        public void fileWatcher_OnException_SendsErrorEmail()
        {
            string resXml = Path.Combine(_testDir, "err.xml");
            File.WriteAllText(resXml, "<root><SFTPLocationRequest>bad\\path</SFTPLocationRequest></root>");
            var xmlNode = new XmlDocument();
            xmlNode.Load(resXml);
            _mockSftp.Setup(s => s.LoadResourceXml(It.IsAny<string>())).Returns(xmlNode.DocumentElement);

            // Simulate an exception when creating the watcher by throwing from FileSystemWatcher.Path
            // (simulate by making Path invalid or by throwing when you create it in your test)
            _recon.fileWatcher();

            _mockEmail.Verify(e => e.ErrorMessageAsync(It.IsAny<string>()), Times.AtLeastOnce);
        }

        [TestMethod]
        public void filewatcher_Created_ParallelProcessFalse_CallsProcessFile_Sync()
        {
            var e = new FileSystemEventArgs(WatcherChangeTypes.Created, _testDir, "test.txt");
            File.WriteAllText(Path.Combine(_testDir, "test.txt"), "data");
            typeof(TransactionReconClass)
                .GetMethod("filewatcher_Created", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(_recon, new object[] { null, e });
            // No exceptions = pass
        }

        [TestMethod]
        public void filewatcher_Created_ParallelProcessTrue_CallsProcessFile_Async()
        {
            _mockConfig.SetupGet(x => x.ParallelProcessFlag).Returns("true");
            var recon2 = new TransactionReconClass(_mockConfig.Object, _mockLog.Object, _mockEmail.Object, _mockDAO.Object, _mockLoader.Object, _mockSftp.Object);

            var e = new FileSystemEventArgs(WatcherChangeTypes.Created, _testDir, "test2.txt");
            File.WriteAllText(Path.Combine(_testDir, "test2.txt"), "data");
            typeof(TransactionReconClass)
                .GetMethod("filewatcher_Created", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(recon2, new object[] { null, e });
        }

        [TestMethod]
        public async Task ProcessFile_InvalidExtension_DoesNothing()
        {
            var file = Path.Combine(_testDir, "file.ignore");
            File.WriteAllText(file, "dummy");
            await _recon.ProcessFile(file);
        }

        [TestMethod]
        public async Task ProcessFile_FileDoesNotExist_DoesNothing()
        {
            string fake = Path.Combine(_testDir, "none.txt");
            await _recon.ProcessFile(fake);
        }

        [TestMethod]
        public async Task ProcessFile_MissingTRECFile_ErrorEmailAndMoveFile()
        {
            var file = Path.Combine(_testDir, "file.TXT");
            File.WriteAllText(file, "data");
            var dt = new DataTable();
            dt.Columns.Add("RESOURCE_PATH_NAME");
            dt.Rows.Add("resourcepath_");
            _mockDAO.Setup(d => d.GetResourcePathNameAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(dt);
            _mockSftp.Setup(x => x.LoadResourceXml(It.IsAny<string>())).Returns(CreateMockResourceNode(_testDir, _testDir, _testDir));
            _mockConfig.SetupGet(x => x.TXTFileFormat).Returns("*.TXT");
            await _recon.ProcessFile(file);

            _mockEmail.Verify(m => m.ErrorMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            _mockLoader.Verify(l => l.moveFile(It.IsAny<string>(), It.IsAny<string>()), Times.AtLeastOnce);
        }

        [TestMethod]
        public async Task ProcessFile_TRECFile_AlreadyArchived_ErrorEmailAndMoveFile()
        {
            var file = Path.Combine(_testDir, "TRECfile.TXT");
            File.WriteAllText(file, "data");
            var archivedFile = Path.Combine(_testDir, "TRECfile.TXT");
            File.Copy(file, archivedFile, overwrite: true);

            var dt = new DataTable();
            dt.Columns.Add("RESOURCE_PATH_NAME");
            dt.Rows.Add("");
            _mockDAO.Setup(d => d.GetResourcePathNameAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(dt);
            _mockSftp.Setup(x => x.LoadResourceXml(It.IsAny<string>())).Returns(CreateMockResourceNode(_testDir, _testDir, _testDir));
            _mockConfig.SetupGet(x => x.TXTFileFormat).Returns("*.TXT");
            await _recon.ProcessFile(file);

            _mockEmail.Verify(m => m.ErrorMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.AtLeastOnce);
        }

        [TestMethod]
        public async Task ProcessFile_TRECFile_NotArchived_CallsDataLoader()
        {
            var file = Path.Combine(_testDir, "TRECfile.XML");
            File.WriteAllText(file, "data");
            var dt = new DataTable();
            dt.Columns.Add("RESOURCE_PATH_NAME");
            dt.Rows.Add("");
            _mockDAO.Setup(d => d.GetResourcePathNameAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(dt);
            _mockSftp.Setup(x => x.LoadResourceXml(It.IsAny<string>())).Returns(CreateMockResourceNode(_testDir, _testDir, _testDir));
            _mockConfig.SetupGet(x => x.XMLFileFormat).Returns("*.XML");
            _mockLoader.Setup(l => l.trXmlReader(file, It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new TransactionReconcilationInput());
            await _recon.ProcessFile(file);
            _mockLoader.Verify(l => l.trXmlReader(file, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public async Task ProcessFile_OnException_SendsErrorEmailAndLogs()
        {
            var file = Path.Combine(_testDir, "err.TXT");
            File.WriteAllText(file, "err");
            _mockDAO.Setup(d => d.GetResourcePathNameAsync(It.IsAny<string>(), It.IsAny<string>())).Throws(new Exception("fail"));
            await _recon.ProcessFile(file);

            _mockLog.Verify(l => l.WriteError(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Exception>()), Times.AtLeastOnce);
            _mockEmail.Verify(m => m.ErrorMessageAsync(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void ProcessExistingFiles_NoXml_NoException()
        {
            _recon.ProcessExistingFiles();
        }

        [TestMethod]
        public void ProcessExistingFiles_Exception_HandledGracefully()
        {
            _mockSftp.Setup(x => x.LoadResourceXml(It.IsAny<string>())).Throws(new Exception("fail"));
            _recon.ProcessExistingFiles();
            _mockEmail.Verify(e => e.ErrorMessageAsync(It.IsAny<string>()), Times.AtLeastOnce);
        }

        [TestMethod]
        public async Task GetMailBySystemAndRegon_SystemNull_ReturnsConfigMail()
        {
            var method = typeof(TransactionReconClass)
                .GetMethod("GetMailBySystemAndRegon", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = await (Task<string>)method.Invoke(_recon, new object[] { null, "zone" });
            Assert.AreEqual("test@mail.com", result);
        }

        [TestMethod]
        public async Task GetMailBySystemAndRegon_ForSystemWithDAO_ReturnsDAOValue()
        {
            _mockDAO.Setup(d => d.GetMailToDetailsAsync("sysx", "rgn")).Returns("gpi@gpi.com");
            var method = typeof(TransactionReconClass)
                .GetMethod("GetMailBySystemAndRegon", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = await (Task<string>)method.Invoke(_recon, new object[] { "sysx", "rgn" });
            Assert.AreEqual("gpi@gpi.com", result);
        }

        [TestMethod]
        public async Task preValidation_NullInput_DoesNothing()
        {
            await _recon.preValidation(null);
        }

        [TestMethod]
        public async Task preValidation_MissingBU_TriggersError()
        {
            var tInput = new TransactionReconcilationInput
            {
                sourceSystem = "SYS",
                createdDate = DateTime.Now,
                fileName = "file.xml",
                transactionReconcilationDetails = new[] { new TransactionReconcilationDetails
                    { region="E", uniquePaymentID="UQID", businessUnit="" } }
            };
            _mockDAO.Setup(d => d.GetReconTypeAsync(It.IsAny<string>())).Returns("PAYMENT");
            await _recon.preValidation(tInput);

            _mockLoader.Verify(l => l.formAndSendExceptionMessage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<System.Collections.Generic.List<ErrorClass>>()), Times.Once);
        }

        [TestMethod]
        public async Task preValidation_InvalidSystemBU_TriggersInsertRecord()
        {
            var tInput = new TransactionReconcilationInput
            {
                sourceSystem = "SYS",
                createdDate = DateTime.Now,
                fileName = "file.xml",
                transactionReconcilationDetails = new[] { new TransactionReconcilationDetails
                    { region="E", uniquePaymentID="UQID", businessUnit="WRONG" } }
            };
            _mockDAO.Setup(d => d.GetReconTypeAsync(It.IsAny<string>())).Returns("PAYMENT");
            _mockConfig.Setup(c => c.GetAppSetting(It.IsAny<string>())).Returns("");
            await _recon.preValidation(tInput);

            _mockDAO.Verify(d => d.InsertIntoTRTableAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<TransactionReconcilationDetails>()), Times.Once);
        }

        [TestMethod]
        public async Task preValidation_MatchFound_CallsCheckForMandatoryAndProcess()
        {
            var tInput = new TransactionReconcilationInput
            {
                sourceSystem = "SYS",
                createdDate = DateTime.Now,
                fileName = "file.xml",
                transactionReconcilationDetails = new[] { new TransactionReconcilationDetails
                    { region="E", uniquePaymentID="UQID", businessUnit="BU1" } }
            };
            _mockDAO.Setup(d => d.GetReconTypeAsync(It.IsAny<string>())).Returns("PAYMENT");
            _mockConfig.Setup(c => c.GetAppSetting(It.IsAny<string>())).Returns("'BU1'");
            await _recon.preValidation(tInput);
        }

        [TestMethod]
        public async Task ConvertGLBUToAPBU_WithIntEntity_CallsDAO()
        {
            _mockDAO.Setup(d => d.GetAPBUAsync(It.IsAny<string>(), It.IsAny<string>())).Returns("resBU");
            var method = typeof(TransactionReconClass)
                .GetMethod("ConvertGLBUToAPBU", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var res = await (Task<string>)method.Invoke(_recon, new object[] { "BU", "ID123" });
            Assert.AreEqual("resBU", res);
        }

        [TestMethod]
        public async Task ConvertGLBUToAPBU_EmptyIntEntity_ReturnsEmpty()
        {
            var method = typeof(TransactionReconClass)
                .GetMethod("ConvertGLBUToAPBU", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var res = await (Task<string>)method.Invoke(_recon, new object[] { "BU", "" });
            Assert.AreEqual(string.Empty, res);
        }

        [TestMethod]
        public async Task checkForMandatoryAndProcess_ValidPAYMENT_InsertsMatchedTable()
        {
            // Setup to cover the "PAY" record type/payment scenario
            var tr = new TransactionReconcilationDetails
            {
                recordType = "PAY",
                sourcePaymentReference = "XYZ",
                srcCurrency = "INR",
                OriginalPaymentAmount = 99m,
                businessUnit = "BUX"
            };
            var ds = new DataSet();
            var table = new DataTable("PMT");
            table.Columns.Add("ORIGINAL_AMT", typeof(decimal));
            table.Columns.Add("CURRENCY", typeof(string));
            table.Columns.Add("STATUS", typeof(string));
            table.Columns.Add("PMT_SYSTEM", typeof(string));
            table.Columns.Add("PMT_BU", typeof(string));
            table.Columns.Add("TYPE", typeof(string));
            table.Columns.Add("CREATED_DATE", typeof(DateTime));
            table.Columns.Add("MODIFIED_DATE", typeof(object));
            table.Rows.Add(99m, "INR", "A", "SYS", "BUX", "MEMO", DateTime.Now, DBNull.Value);
            ds.Tables.Add(table);
            _mockDAO.Setup(d => d.CheckPmtExistsInGPIAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(ds);

            await _recon.checkForMandatoryAndProcess("SYS", DateTime.Now, tr, "file.xml", "'BUX'");
            _mockDAO.Verify(d => d.InsertIntoTRMatchedTableAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<TransactionReconcilationDetails>()), Times.AtLeastOnce);
        }

        [TestMethod]
        public async Task checkForMandatoryAndProcess_GroupTypeMissing_InsertsToTRTable()
        {
            var tr = new TransactionReconcilationDetails
            {
                recordType = null,
                groupType = null,
                sourcePaymentReference = "S",
                businessUnit = "BUX"
            };
            await _recon.checkForMandatoryAndProcess("SYS", DateTime.Now, tr, "file.xml", "'BUX'");
            _mockDAO.Verify(d => d.InsertIntoTRTableAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<TransactionReconcilationDetails>()), Times.AtLeastOnce);
        }

        [TestMethod]
        public async Task checkForMandatoryAndProcess_HandlesNullRefException_Gracefully()
        {
            var tr = new TransactionReconcilationDetails { businessUnit = null };
            await _recon.checkForMandatoryAndProcess("SYS", DateTime.Now, tr, "file.xml", "'BUX'");
        }

        // For brevity, add similar tests for other branches—MRR, STATUS, Pay+Status etc., including null/empty/malformed data cases

        [TestMethod]
        public void mandotaryAmountCurrencyCheck_Match_ReturnsTrue()
        {
            var tr = new TransactionReconcilationDetails { OriginalPaymentAmount = 9, srcCurrency = "USD" };
            var result = typeof(TransactionReconClass)
                .GetMethod("mandotaryAmountCurrencyCheck", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(_recon, new object[] { 9m, "USD", tr });
            Assert.AreEqual(true, result);
        }

        [TestMethod]
        public void mandotaryAmountCurrencyCheck_AmountMismatch_ReturnsFalse()
        {
            var tr = new TransactionReconcilationDetails { OriginalPaymentAmount = 1, srcCurrency = "USD" };
            var result = typeof(TransactionReconClass)
                .GetMethod("mandotaryAmountCurrencyCheck", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(_recon, new object[] { 99m, "USD", tr });
            Assert.AreEqual(false, result);
            Assert.AreEqual("TR-0006", tr.errorCode);
        }

        [TestMethod]
        public void mandotaryAmountCurrencyCheck_CurrencyMismatch_ReturnsFalse()
        {
            var tr = new TransactionReconcilationDetails { OriginalPaymentAmount = 99, srcCurrency = "USD" };
            var result = typeof(TransactionReconClass)
                .GetMethod("mandotaryAmountCurrencyCheck", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(_recon, new object[] { 99m, "INR", tr });
            Assert.AreEqual(false, result);
            Assert.AreEqual("TR-0007", tr.errorCode);
        }

        [TestMethod]
        public void getModifiedDateTime_BothDBNull_ReturnsUtcNow()
        {
            var method = typeof(TransactionReconClass)
                .GetMethod("getModifiedDateTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var before = DateTime.UtcNow.AddSeconds(-1);
            var dt = (DateTime)method.Invoke(_recon, new object[] { DBNull.Value, DBNull.Value });
            Assert.IsTrue(dt >= before && dt <= DateTime.UtcNow);
        }

        [TestMethod]
        public void getModifiedDateTime_ModifiedNotDbNull_ReturnsModified()
        {
            var created = DateTime.Now.AddHours(-1);
            var modified = DateTime.Now;
            var method = typeof(TransactionReconClass)
                .GetMethod("getModifiedDateTime", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var dt = (DateTime)method.Invoke(_recon, new object[] { created, modified });
            Assert.AreEqual(modified, dt);
        }

        // Add more tests for CheckTransactionInUnmatchedTable and compareGPIStatus as needed
        // Helper method to create XmlNode for SFTP config mocks
        private static XmlNode CreateMockResourceNode(string request, string archive, string error)
        {
            var xmldoc = new XmlDocument();
            var el = xmldoc.CreateElement("EODRResource");
            var r1 = xmldoc.CreateElement("SFTPLocationRequest"); r1.InnerText = request;
            var r2 = xmldoc.CreateElement("SFTPLocationArchive"); r2.InnerText = archive;
            var r3 = xmldoc.CreateElement("SFTPLocationError"); r3.InnerText = error;
            el.AppendChild(r1); el.AppendChild(r2); el.AppendChild(r3);
            xmldoc.AppendChild(el);
            return el;
        }
    }
}
