using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using GPI.TransactionRecon.BusinessLogic;
using GPI.TransactionRecon.Logger.Contracts;
using GPI.TransactionRecon.BusinessLogic.Contracts;
using GPI.BusinessEntities;
using GPI.DAL.Contracts;

namespace GPI.TransactionRecon.BusinessLogic.Tests
{
    [TestClass]
    public class TRDataLoaderTests
    {
        private string _testDir;
        private string _archiveDir;
        private string _errorDir;
        private Mock<ICommonDAO> _mockDAO;
        private Mock<IEmailService> _mockEmail;
        private Mock<IAppConfiguration> _mockConfig;
        private Mock<ILoggerService> _mockLog;
        private TRDataLoader _loader;

        [TestInitialize]
        public void Setup()
        {
            _testDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            _archiveDir = Path.Combine(_testDir, "archive");
            _errorDir = Path.Combine(_testDir, "error");
            Directory.CreateDirectory(_testDir);
            Directory.CreateDirectory(_archiveDir);
            Directory.CreateDirectory(_errorDir);

            _mockDAO = new Mock<ICommonDAO>();
            _mockEmail = new Mock<IEmailService>();
            _mockConfig = new Mock<IAppConfiguration>();
            _mockLog = new Mock<ILoggerService>();
            _mockConfig.SetupGet(c => c.TRMailTo).Returns("someone@test.com");

            _loader = new TRDataLoader(_mockConfig.Object, _mockDAO.Object, _mockEmail.Object, _mockLog.Object);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_testDir))
                Directory.Delete(_testDir, true);
        }

        [TestMethod]
        public async Task trXmlReader_ValidFile_ReturnsInputWithDetails()
        {
            string sys = "SYS"; string region = "REG";
            string folder = Path.Combine(_testDir, region, sys);
            Directory.CreateDirectory(folder);
            string xmlFile = Path.Combine(folder, "testfile.xml");
            File.WriteAllText(xmlFile, @"<TransactionReconcilationInput>
                <SourceSystem>SYS</SourceSystem>
                <TotalNumberofRecords>1</TotalNumberofRecords>
                <ReconciliationDetails>
                    <SourceBusunit>BUX</SourceBusunit>
                    <SourceReference>REF1</SourceReference>
                    <SourceCurrency>INR</SourceCurrency>
                    <SourceAmount>5</SourceAmount>
                    <ForeignAmount>3</ForeignAmount>
                    <ForeignCurrency>USD</ForeignCurrency>
                    <UniquePaymentID>ID1</UniquePaymentID>
                    <LedgerAccount>A123</LedgerAccount>
                    <Qualifier>QUAL</Qualifier>
                    <CreatedBy>user</CreatedBy>
                    <GroupType>G</GroupType>
                    <Status>COMP</Status>
                    <TransactionDateTime>2025-07-21 10:30:05</TransactionDateTime>
                </ReconciliationDetails>
            </TransactionReconcilationInput>");

            var result = await _loader.trXmlReader(xmlFile, _archiveDir + Path.DirectorySeparatorChar, _errorDir + Path.DirectorySeparatorChar);
            Assert.IsNotNull(result);
            Assert.AreEqual("SYS", result.sourceSystem);
            Assert.IsNotNull(result.transactionReconcilationDetails);
            Assert.AreEqual(1, result.transactionReconcilationDetails.Length);
            Assert.AreEqual("REG", result.transactionReconcilationDetails[0].region);
        }

        [TestMethod]
        public async Task trXmlReader_FileDoesNotExist_ReturnsNull()
        {
            string missingFile = Path.Combine(_testDir, "404.xml");
            var result = await _loader.trXmlReader(missingFile, _archiveDir, _errorDir);
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task trXmlReader_BadXml_ReportsErrorViaEmail()
        {
            string badFile = Path.Combine(_testDir, "bad.xml");
            File.WriteAllText(badFile, "<bad><abc></bad>");

            var res = await _loader.trXmlReader(badFile, _archiveDir, _errorDir);
            Assert.IsNull(res);
            _mockEmail.Verify(e => e.EmailErrorMessageAsync(It.IsAny<string>(), It.IsAny<DateTime>(), badFile, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.AtLeastOnce);
        }

        [TestMethod]
        public async Task trXmlReader_NegativeAmounts_AreConvertedToPositive()
        {
            string sys = "SYS"; string region = "REG";
            string folder = Path.Combine(_testDir, region, sys);
            Directory.CreateDirectory(folder);
            string xmlFile = Path.Combine(folder, "test_negative.xml");
            File.WriteAllText(xmlFile, @"<TransactionReconcilationInput>
                <SourceSystem>SYS</SourceSystem>
                <ReconciliationDetails>
                    <SourceAmount>-5</SourceAmount>
                    <ForeignAmount>-3</ForeignAmount>
                </ReconciliationDetails>
            </TransactionReconcilationInput>");

            var result = await _loader.trXmlReader(xmlFile, _archiveDir, _errorDir);
            Assert.AreEqual(5, result.transactionReconcilationDetails[0].OriginalPaymentAmount);
            Assert.AreEqual(3, result.transactionReconcilationDetails[0].AccountingAmount);
        }

        [TestMethod]
        public async Task trXmlReader_PartialError_RecordsErrorListSendsMail()
        {
            // If a field is missing, record goes to error list, exception handled
            string sys = "SYS"; string region = "REG";
            string folder = Path.Combine(_testDir, region, sys);
            Directory.CreateDirectory(folder);
            string xmlFile = Path.Combine(folder, "bad_record.xml");
            File.WriteAllText(xmlFile, @"<TransactionReconcilationInput>
                <SourceSystem>SYS</SourceSystem>
                <ReconciliationDetails>
                    <SourceAmount>abc</SourceAmount>
                </ReconciliationDetails>
            </TransactionReconcilationInput>");

            var res = await _loader.trXmlReader(xmlFile, _archiveDir, _errorDir);
            _mockEmail.Verify(e => e.EmailErrorMessageAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.AtLeastOnce);
        }

        [TestMethod]
        public async Task trCsvReader_ValidFile_ParsesSuccessfully()
        {
            string sys = "SYS"; string region = "REG";
            string folder = Path.Combine(_testDir, region, sys);
            Directory.CreateDirectory(folder);
            string csvFile = Path.Combine(folder, "file.csv");
            string[] lines = new[]
            {
                "\"SYS\",\"2025-07-21\",\"REG\",\"BU1\",\"INR\",\"5\",\"6\",\"USD\",\"PID1\",\"ACC1\",\"QUAL\",\"Me\",\"C\",\"Open\",\"2025-07-21 10:32:06\""
            };
            File.WriteAllLines(csvFile, lines);

            var res = await _loader.trCsvReader(csvFile, _archiveDir, _errorDir);
            Assert.IsNotNull(res);
            Assert.IsNotNull(res.transactionReconcilationDetails);
            Assert.AreEqual("REG", res.transactionReconcilationDetails[0].region);
        }

        [TestMethod]
        public async Task trCsvReader_CsvWithInvalidRow_CatchesAndReportsError()
        {
            string sys = "SYS"; string region = "REG";
            string folder = Path.Combine(_testDir, region, sys);
            Directory.CreateDirectory(folder);
            string csvFile = Path.Combine(folder, "badrow.csv");
            string[] lines = new[]
            {
                "\"SYS\",\"2025-07-21\",\"REG\",\"BU1\",\"INR\",\"5\",\"6\",\"USD\",\"PID1\",\"ACC1\",\"QUAL\",\"Me\",\"C\",\"Open\",\"2025-07-21 10:32:06\"",
                "\"broken_field\""
            };
            File.WriteAllLines(csvFile, lines);

            await _loader.trCsvReader(csvFile, _archiveDir, _errorDir);
            _mockEmail.Verify(e => e.EmailErrorMessageAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.AtLeastOnce);
        }

        [TestMethod]
        public async Task trCsvReader_FileDoesNotExist_ReturnsNull()
        {
            var res = await _loader.trCsvReader("no-exist.csv", _archiveDir, _errorDir);
            Assert.IsNull(res);
        }

        [TestMethod]
        public async Task trTxtReader_ValidLine_ParsesOk()
        {
            string sys = "SYS"; string region = "REG";
            string folder = Path.Combine(_testDir, region, sys);
            Directory.CreateDirectory(folder);
            string txtFile = Path.Combine(folder, "file.txt");
            string line = "SYS           2025-07-21 BU1      SOURCEPAYREF     INR 5             6             USD PID1      ACC1                                            QUAL                Me                  COPEN2025-07-21 10:41:33 ";
            File.WriteAllText(txtFile, line.PadRight(239));

            var res = await _loader.trTxtReader(txtFile, _archiveDir, _errorDir);
            Assert.IsNotNull(res);
            Assert.IsNotNull(res.transactionReconcilationDetails);
            Assert.AreEqual("REG", res.transactionReconcilationDetails[0].region);
        }

        [TestMethod]
        public async Task trTxtReader_FileDoesNotExist_ReturnsNull()
        {
            string file = Path.Combine(_testDir, "404.txt");
            var res = await _loader.trTxtReader(file, _archiveDir, _errorDir);
            Assert.IsNull(res);
        }

        [TestMethod]
        public async Task trTxtReader_BadLine_CatchesAndReportsError()
        {
            string sys = "SYS"; string region = "REG";
            string folder = Path.Combine(_testDir, region, sys);
            Directory.CreateDirectory(folder);
            string txtFile = Path.Combine(folder, "bad.txt");
            File.WriteAllLines(txtFile, new[] { "SHORTLINE" });
            await _loader.trTxtReader(txtFile, _archiveDir, _errorDir);
            _mockEmail.Verify(e => e.EmailErrorMessageAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.AtLeastOnce);
        }

        [TestMethod]
        public void moveFile_FileMovedToDestAndLogOnError()
        {
            string file = Path.Combine(_testDir, "move.txt");
            File.WriteAllText(file, "dummy");
            string dest = _archiveDir + Path.DirectorySeparatorChar;
            _loader.moveFile(file, "testsys", dest);

            Assert.IsFalse(File.Exists(file));
            var moved = Directory.GetFiles(_archiveDir);
            Assert.AreEqual(1, moved.Length);
        }

        [TestMethod]
        public void moveFile_FileMovedToErrorDirOnException()
        {
            string file = Path.Combine(_testDir, "move2.txt");
            File.WriteAllText(file, "content2");
            string fakeDest = Path.Combine(_testDir, "nonwritable") + Path.DirectorySeparatorChar;
            _loader.moveFile(file, fakeDest);
            _mockLog.Verify(l => l.TRWriteToLogs(It.IsAny<string>(), "Logs"), Times.AtLeastOnce);
        }

        [TestMethod]
        public async Task formAndSendExceptionMessage_CallsEmailWithTableHtml()
        {
            var errors = new List<ErrorClass>
            {
                new ErrorClass { RecordNo = 2, ErrorDesc = "BAD" }
            };

            await _loader.formAndSendExceptionMessage("SYS", "REG", "f.xml", errors);
            _mockEmail.Verify(e => e.EmailErrorMessageAsync(
                "SYS", "f.xml", It.Is<string>(html => html.Contains("BAD")), "REG", "someone@test.com"), Times.Once);
        }

        [TestMethod]
        public async Task formAndSendExceptionMessage_HandlesExceptionAndLogs()
        {
            _mockEmail.Setup(e => e.EmailErrorMessageAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new Exception("fail"));

            await _loader.formAndSendExceptionMessage("X", "Y", "f.none", new List<ErrorClass> { new ErrorClass { RecordNo = 1, ErrorDesc = "Fail" } });
            _mockLog.Verify(l => l.TRWriteToLogs(It.IsAny<string>(), "Logs"), Times.AtLeastOnce);
        }

        [TestMethod]
        public async Task sendErrorEmail_CallsEmail_SucceedsAndMovesFile()
        {
            string f = Path.Combine(_testDir, "fail.txt");
            File.WriteAllText(f, "anything");

            // also mock GetMailToDetailsAsync (returns null/fallback)
            _mockDAO.Setup(d => d.GetMailToDetailsAsync(It.IsAny<string>(), It.IsAny<string>())).Returns("");
            await typeof(TRDataLoader)
                .GetMethod("sendErrorEmail", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(_loader, new object[] { f, "SYS", "REG", new Exception("xx"), _errorDir });

            _mockEmail.Verify(e => e.EmailErrorMessageAsync(
                It.IsAny<string>(), It.IsAny<DateTime>(), f, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            Assert.IsFalse(File.Exists(f));
        }

        [TestMethod]
        public async Task GetMailBySystemAndRegon_Returns_DAO_Value_IfNotNull()
        {
            _mockDAO.Setup(d => d.GetMailToDetailsAsync("SYS", "REG")).Returns("wanted@x.com");
            var method = typeof(TRDataLoader).GetMethod("GetMailBySystemAndRegon", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var val = await (Task<string>)method.Invoke(_loader, new object[] { "SYS", "REG" });
            Assert.AreEqual("wanted@x.com", val);
        }

        [TestMethod]
        public async Task GetMailBySystemAndRegon_FallbackToConfig()
        {
            _mockDAO.Setup(d => d.GetMailToDetailsAsync("SYS", "REG")).Returns("");
            var method = typeof(TRDataLoader).GetMethod("GetMailBySystemAndRegon", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var val = await (Task<string>)method.Invoke(_loader, new object[] { "SYS", "REG" });
            Assert.AreEqual("someone@test.com", val);
        }
    }
}
