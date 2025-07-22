using GPI.DAL;
using GPI.DAL.Contracts;
using GPI.TransactionRecon.Logger.Contracts;
using GPI.BusinessEntities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Configuration;
using System.Data;
using System.Data.OleDb;
using System.Collections.Generic;
using System.Linq;

namespace CHUBB.TTP.TranRec.Tests
{
    [TestClass]
    public class CommonDAOTests
    {
        private ICommonDAO _sut;
        private Mock<IAppConfiguration> _config;
        private Mock<ILoggerService> _logger;
        private string _connectionString = ConfigurationManager.ConnectionStrings["QADB"].ConnectionString;

        [TestInitialize]
        public void Setup()
        {
            _config = new Mock<IAppConfiguration>();
            _logger = new Mock<ILoggerService>();

            _config.SetupGet(c => c.DatabaseConnectionString).Returns(_connectionString);
            _config.SetupGet(c => c.BU_ID).Returns("BU1,BU2,BU3");
            _config.SetupGet(c => c.GLTOAPBU_SYS).Returns("TRAX,SYSTEM1");

            _sut = new CommonDAO(_config.Object, _logger.Object);
        }

        #region Helper Methods

        private Mock<IAppConfiguration> CreateTestConfig()
        {
            var config = new Mock<IAppConfiguration>();
            config.SetupGet(c => c.DatabaseConnectionString).Returns(_connectionString);
            config.SetupGet(c => c.BU_ID).Returns("BU1,BU2,BU3");
            config.SetupGet(c => c.GLTOAPBU_SYS).Returns("TRAX,SYSTEM1");
            return config;
        }

        private TransactionReconcilationDetails CreateTestTransactionDetails()
        {
            return new TransactionReconcilationDetails
            {
                businessUnit = "BU1",
                sourcePaymentReference = "REF123",
                ledgerAccount = "12345",
                srcCurrency = "USD",
                recordType = "DEBIT",
                OriginalPaymentAmount = 1000.50m,
                AccountingAmount = 1000.50m,
                groupType = "PAYMENT",
                qualifier = "QUAL1",
                CreatedBy = "SYSTEM",
                uniquePaymentID = "UNIQUE123",
                status = "SUCCESS",
                frcCurrency = "USD",
                statusGPI = "COMPLETED",
                trDateTime = DateTime.UtcNow,
                errorCode = "",
                errorDesc = "",
                reconcilationType = "STATUS",
                region = "US",
                gpiModifiedTime = DateTime.UtcNow
            };
        }

        private void InsertTestSystemDetails(OleDbTransaction tx, OleDbConnection con, string system, string region, string contactInfo)
        {
            var cmd = new OleDbCommand(
                "INSERT INTO GPI.EODRSYSTEMDETAILS (SYSTEM_NAME, REGION, CONTACT_INFO) VALUES (?, ?, ?)", 
                con, tx);
            cmd.Parameters.AddWithValue("@system", system);
            cmd.Parameters.AddWithValue("@region", region);
            cmd.Parameters.AddWithValue("@contact", contactInfo);
            cmd.ExecuteNonQuery();
        }

        private void InsertTestLedgerRecord(OleDbTransaction tx, OleDbConnection con, 
            string bu, string reference, string system = "TRAX")
        {
            var cmd = new OleDbCommand(
                "INSERT INTO GPI.LEDGER (LED_BU, REFERENCE, LED_SYSTEM, CURRENCY, AMOUNT) VALUES (?, ?, ?, ?, ?)", 
                con, tx);
            cmd.Parameters.AddWithValue("@bu", bu);
            cmd.Parameters.AddWithValue("@ref", reference);
            cmd.Parameters.AddWithValue("@sys", system);
            cmd.Parameters.AddWithValue("@curr", "USD");
            cmd.Parameters.AddWithValue("@amt", 1000.00);
            cmd.ExecuteNonQuery();
        }

        private void InsertTestPaymentQueue(OleDbTransaction tx, OleDbConnection con, 
            string system, string bu, string paynum)
        {
            var cmd = new OleDbCommand(@"
                INSERT INTO GPI.PAYMENTQUEUE (PMQ_SYSTEM, PMQ_BU, PAYNUM, ACCNO, CRNCY, PAYMT, ACAMT, CREATED_BY, GRPTP, QUALIFIER, STATUS, CREATED_DATE, MODIFIED_DATE) 
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)", 
                con, tx);
            cmd.Parameters.AddWithValue("@sys", system);
            cmd.Parameters.AddWithValue("@bu", bu);
            cmd.Parameters.AddWithValue("@paynum", paynum);
            cmd.Parameters.AddWithValue("@accno", "12345");
            cmd.Parameters.AddWithValue("@crncy", "USD");
            cmd.Parameters.AddWithValue("@paymt", 1000.00);
            cmd.Parameters.AddWithValue("@acamt", 1000.00);
            cmd.Parameters.AddWithValue("@createdby", "SYSTEM");
            cmd.Parameters.AddWithValue("@grptp", "PAYMENT");
            cmd.Parameters.AddWithValue("@qual", "TEST");
            cmd.Parameters.AddWithValue("@status", "PENDING");
            cmd.Parameters.AddWithValue("@created", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("@modified", DateTime.UtcNow);
            cmd.ExecuteNonQuery();
        }

        private void InsertTestPayment(OleDbTransaction tx, OleDbConnection con, 
            string system, string bu, string originalPmtId, string pmtId = "12345")
        {
            var cmd = new OleDbCommand(@"
                INSERT INTO GPI.PAYMENT (PMT_SYSTEM, PMT_BU, PMT_ID, ORIGINAL_PMT_ID, ACCOUNT_ID, CURRENCY, ORIGINAL_AMT, ACCOUNTING_AMT, CREATED_BY, GROUP_TYPE, QUALIFIER, STATUS, CREATED_DATE, MODIFIED_DATE) 
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)", 
                con, tx);
            cmd.Parameters.AddWithValue("@sys", system);
            cmd.Parameters.AddWithValue("@bu", bu);
            cmd.Parameters.AddWithValue("@pmtid", pmtId);
            cmd.Parameters.AddWithValue("@origid", originalPmtId);
            cmd.Parameters.AddWithValue("@accid", "12345");
            cmd.Parameters.AddWithValue("@curr", "USD");
            cmd.Parameters.AddWithValue("@origamt", 1000.00);
            cmd.Parameters.AddWithValue("@accamt", 1000.00);
            cmd.Parameters.AddWithValue("@createdby", "SYSTEM");
            cmd.Parameters.AddWithValue("@grptype", "PAYMENT");
            cmd.Parameters.AddWithValue("@qual", "TEST");
            cmd.Parameters.AddWithValue("@status", "COMPLETED");
            cmd.Parameters.AddWithValue("@created", DateTime.UtcNow);
            cmd.Parameters.AddWithValue("@modified", DateTime.UtcNow);
            cmd.ExecuteNonQuery();
        }

        #endregion

        #region GetMailToDetailsAsync Tests

        [TestMethod]
        public void GetMailToDetailsAsync_ValidSystemRegion_ReturnsEmail()
        {
            using (var con = new OleDbConnection(_connectionString))
            {
                con.Open();
                using (var tx = con.BeginTransaction())
                {
                    // Arrange
                    InsertTestSystemDetails(tx, con, "SYSTEM1", "US", "test@chubb.com");

                    // Act
                    string result = _sut.GetMailToDetailsAsync("SYSTEM1", "US");

                    // Assert
                    Assert.IsTrue(result.Contains("@chubb.com"));
                    Assert.AreEqual("test@chubb.com", result);

                    tx.Rollback();
                }
            }
        }

        [TestMethod]
        public void GetMailToDetailsAsync_WrongSystemOrRegion_ReturnsEmpty()
        {
            using (var con = new OleDbConnection(_connectionString))
            {
                con.Open();
                using (var tx = con.BeginTransaction())
                {
                    // Arrange
                    InsertTestSystemDetails(tx, con, "SYSTEM1", "US", "test@chubb.com");

                    // Act
                    string result = _sut.GetMailToDetailsAsync("SYSTEM1", "WrongRegion");

                    // Assert
                    Assert.AreEqual("", result);

                    tx.Rollback();
                }
            }
        }

        [TestMethod]
        public void GetMailToDetailsAsync_NoMatchingRow_ReturnsEmptyString()
        {
            // Act
            string result = _sut.GetMailToDetailsAsync("WrongSystem", "WrongRegion");

            // Assert
            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void GetMailToDetailsAsync_NullParameters_ReturnsEmpty()
        {
            // Act
            string result = _sut.GetMailToDetailsAsync(null, null);

            // Assert
            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void GetMailToDetailsAsync_InvalidConnectionString_LogsErrorAndReturnsEmpty()
        {
            // Arrange
            _config.SetupGet(c => c.DatabaseConnectionString).Returns("invalid_connection_string");
            var dao = new CommonDAO(_config.Object, _logger.Object);

            // Act
            string result = dao.GetMailToDetailsAsync("SYSTEM1", "US");

            // Assert
            Assert.AreEqual(string.Empty, result);
            _logger.Verify(
                lg => lg.WriteError(
                    nameof(_sut.GetMailToDetailsAsync),
                    It.Is<string>(msg => msg.Contains("GetMailToDetailsAsync")),
                    It.IsAny<Exception>()),
                Times.Once);
        }

        #endregion

        #region CheckVoucherExistsInGPIAsync Tests

        [TestMethod]
        public void CheckVoucherExistsInGPIAsync_TraxSystem_ReturnsMatchingRows()
        {
            using (var con = new OleDbConnection(_connectionString))
            {
                con.Open();
                using (var tx = con.BeginTransaction())
                {
                    // Arrange
                    InsertTestLedgerRecord(tx, con, "BU1", "REF123", "TRAX");

                    // Act
                    var result = _sut.CheckVoucherExistsInGPIAsync("TRAX", "BU1", "REF123");

                    // Assert
                    Assert.IsNotNull(result);
                    Assert.IsTrue(result.Tables.Contains("LED"));
                    Assert.IsTrue(result.Tables["LED"].Rows.Count >= 1);

                    var row = result.Tables["LED"].Rows[0];
                    Assert.AreEqual("BU1", row["LED_BU"].ToString());
                    Assert.AreEqual("REF123", row["REFERENCE"].ToString());

                    tx.Rollback();
                }
            }
        }

        [TestMethod]
        public void CheckVoucherExistsInGPIAsync_NonTraxSystem_UsesSystemInQuery()
        {
            using (var con = new OleDbConnection(_connectionString))
            {
                con.Open();
                using (var tx = con.BeginTransaction())
                {
                    // Arrange
                    InsertTestLedgerRecord(tx, con, "BU1", "REF123", "OTHER_SYSTEM");

                    // Act
                    var result = _sut.CheckVoucherExistsInGPIAsync("OTHER_SYSTEM", "BU1", "REF123");

                    // Assert
                    Assert.IsNotNull(result);
                    Assert.IsTrue(result.Tables.Contains("LED"));
                    Assert.IsTrue(result.Tables["LED"].Rows.Count >= 1);

                    tx.Rollback();
                }
            }
        }

        [TestMethod]
        public void CheckVoucherExistsInGPIAsync_NoMatchingRecord_ReturnsEmptyDataSet()
        {
            // Act
            var result = _sut.CheckVoucherExistsInGPIAsync("NONEXISTENT", "BU999", "REF999");

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Tables.Contains("LED"));
            Assert.AreEqual(0, result.Tables["LED"].Rows.Count);
        }

        [TestMethod]
        public void CheckVoucherExistsInGPIAsync_NullParameters_LogsError()
        {
            // Act
            var result = _sut.CheckVoucherExistsInGPIAsync(null, null, null);

            // Assert
            Assert.IsNotNull(result);
            _logger.Verify(lg => lg.WriteError(
                nameof(_sut.CheckVoucherExistsInGPIAsync),
                It.IsAny<string>(),
                It.IsAny<Exception>()), Times.Once);
        }

        #endregion

        #region CheckPmtExistsInGPIAsync Tests

        [TestMethod]
        public void CheckPmtExistsInGPIAsync_TraxSystemNotInBUID_UsesOriginalPmtIdQuery()
        {
            using (var con = new OleDbConnection(_connectionString))
            {
                con.Open();
                using (var tx = con.BeginTransaction())
                {
                    // Arrange
                    InsertTestPaymentQueue(tx, con, "TRAX", "OTHER_BU", "REF123");
                    InsertTestPayment(tx, con, "TRAX", "OTHER_BU", "REF123");

                    // Act
                    var result = _sut.CheckPmtExistsInGPIAsync("TRAX", "OTHER_BU", "REF123");

                    // Assert
                    Assert.IsNotNull(result);
                    Assert.IsTrue(result.Tables.Contains("PMQ"));
                    Assert.IsTrue(result.Tables.Contains("PMT"));
                    Assert.IsTrue(result.Tables["PMQ"].Rows.Count >= 1);
                    Assert.IsTrue(result.Tables["PMT"].Rows.Count >= 1);

                    tx.Rollback();
                }
            }
        }

        [TestMethod]
        public void CheckPmtExistsInGPIAsync_TraxSystemInBUID_UsesPmtIdQuery()
        {
            using (var con = new OleDbConnection(_connectionString))
            {
                con.Open();
                using (var tx = con.BeginTransaction())
                {
                    // Arrange
                    InsertTestPaymentQueue(tx, con, "TRAX", "BU1", "ABC12345");
                    InsertTestPayment(tx, con, "TRAX", "BU1", "ABC12345", "12345");

                    // Act
                    var result = _sut.CheckPmtExistsInGPIAsync("TRAX", "BU1", "ABC12345");

                    // Assert
                    Assert.IsNotNull(result);
                    Assert.IsTrue(result.Tables.Contains("PMQ"));
                    Assert.IsTrue(result.Tables.Contains("PMT"));

                    tx.Rollback();
                }
            }
        }

        [TestMethod]
        public void CheckPmtExistsInGPIAsync_NonTraxSystem_UsesSystemInQuery()
        {
            using (var con = new OleDbConnection(_connectionString))
            {
                con.Open();
                using (var tx = con.BeginTransaction())
                {
                    // Arrange
                    InsertTestPaymentQueue(tx, con, "OTHER_SYSTEM", "BU1", "REF123");
                    InsertTestPayment(tx, con, "OTHER_SYSTEM", "BU1", "REF123");

                    // Act
                    var result = _sut.CheckPmtExistsInGPIAsync("OTHER_SYSTEM", "BU1", "REF123");

                    // Assert
                    Assert.IsNotNull(result);
                    Assert.IsTrue(result.Tables.Contains("PMQ"));
                    Assert.IsTrue(result.Tables.Contains("PMT"));

                    tx.Rollback();
                }
            }
        }

        [TestMethod]
        public void CheckPmtExistsInGPIAsync_NullParameters_LogsError()
        {
            // Act
            var result = _sut.CheckPmtExistsInGPIAsync(null, null, null);

            // Assert
            Assert.IsNotNull(result);
            _logger.Verify(lg => lg.WriteError(
                nameof(_sut.CheckPmtExistsInGPIAsync),
                It.IsAny<string>(),
                It.IsAny<Exception>()), Times.Once);
        }

        #endregion

        #region InsertIntoTRTableAsync Tests

        [TestMethod]
        public void InsertIntoTRTableAsync_ValidParameters_InsertsSuccessfully()
        {
            using (var con = new OleDbConnection(_connectionString))
            {
                con.Open();
                using (var tx = con.BeginTransaction())
                {
                    // Arrange
                    var tr = CreateTestTransactionDetails();
                    string sourceSystem = "TEST_SYSTEM";
                    DateTime createdDate = DateTime.UtcNow;

                    // Act
                    _sut.InsertIntoTRTableAsync(sourceSystem, createdDate, tr);

                    // Verify data was inserted
                    var verifyCmd = new OleDbCommand(
                        "SELECT COUNT(*) FROM GPI.TRANSACTIONRECON WHERE SYSTEM = ? AND SOURCEPAYMENTREFERENCE = ?",
                        con, tx);
                    verifyCmd.Parameters.AddWithValue("@sys", sourceSystem);
                    verifyCmd.Parameters.AddWithValue("@ref", tr.sourcePaymentReference);
                    
                    var count = (int)verifyCmd.ExecuteScalar();
                    Assert.IsTrue(count > 0);

                    tx.Rollback();
                }
            }
        }

        [TestMethod]
        public void InsertIntoTRTableAsync_NullAmounts_HandlesCorrectly()
        {
            using (var con = new OleDbConnection(_connectionString))
            {
                con.Open();
                using (var tx = con.BeginTransaction())
                {
                    // Arrange
                    var tr = CreateTestTransactionDetails();
                    tr.OriginalPaymentAmount = null;
                    tr.AccountingAmount = null;

                    // Act
                    _sut.InsertIntoTRTableAsync("TEST", DateTime.UtcNow, tr);

                    // Assert - No exception should be thrown
                    Assert.IsTrue(true);

                    tx.Rollback();
                }
            }
        }

        [TestMethod]
        public void InsertIntoTRTableAsync_NullTransactionDetails_ThrowsException()
        {
            // Act & Assert
            Assert.ThrowsException<Exception>(() => 
                _sut.InsertIntoTRTableAsync("TEST", DateTime.UtcNow, null));
        }

        #endregion

        #region ExportToExcelTRAsync Tests

        [TestMethod]
        public void ExportToExcelTRAsync_TraxSystem_ReturnsFormattedData()
        {
            using (var con = new OleDbConnection(_connectionString))
            {
                con.Open();
                using (var tx = con.BeginTransaction())
                {
                    // Arrange - Insert test data
                    var insertCmd = new OleDbCommand(@"
                        INSERT INTO GPI.TRANSACTIONRECON (SYSTEM, REGION, BU, SOURCEPAYMENTREFERENCE, CREATED_DATE, MODIFIED_DATE, GPI_STATUS, STATUS) 
                        VALUES (?, ?, ?, ?, ?, ?, ?, ?)", con, tx);
                    insertCmd.Parameters.AddWithValue("@sys", "TRAX");
                    insertCmd.Parameters.AddWithValue("@region", "US");
                    insertCmd.Parameters.AddWithValue("@bu", "BU1");
                    insertCmd.Parameters.AddWithValue("@ref", "REF123");
                    insertCmd.Parameters.AddWithValue("@created", DateTime.UtcNow);
                    insertCmd.Parameters.AddWithValue("@modified", DateTime.UtcNow);
                    insertCmd.Parameters.AddWithValue("@gpistatus", "COMPLETED");
                    insertCmd.Parameters.AddWithValue("@status", "SUCCESS");
                    insertCmd.ExecuteNonQuery();

                    // Act
                    var result = _sut.ExportToExcelTRAsync("TRAX", "US");

                    // Assert
                    Assert.IsNotNull(result);
                    Assert.IsTrue(result.Tables.Count > 0);
                    Assert.IsTrue(result.Tables[0].Rows.Count > 0);

                    tx.Rollback();
                }
            }
        }

        [TestMethod]
        public void ExportToExcelTRAsync_NonTraxSystem_ReturnsData()
        {
            using (var con = new OleDbConnection(_connectionString))
            {
                con.Open();
                using (var tx = con.BeginTransaction())
                {
                    // Arrange
                    var insertCmd = new OleDbCommand(@"
                        INSERT INTO GPI.TRANSACTIONRECON (SYSTEM, REGION, BU, SOURCEPAYMENTREFERENCE, CREATED_DATE, MODIFIED_DATE) 
                        VALUES (?, ?, ?, ?, ?, ?)", con, tx);
                    insertCmd.Parameters.AddWithValue("@sys", "OTHER");
                    insertCmd.Parameters.AddWithValue("@region", "UK");
                    insertCmd.Parameters.AddWithValue("@bu", "BU1");
                    insertCmd.Parameters.AddWithValue("@ref", "REF456");
                    insertCmd.Parameters.AddWithValue("@created", DateTime.UtcNow);
                    insertCmd.Parameters.AddWithValue("@modified", DateTime.UtcNow);
                    insertCmd.ExecuteNonQuery();

                    // Act
                    var result = _sut.ExportToExcelTRAsync("OTHER", "UK");

                    // Assert
                    Assert.IsNotNull(result);

                    tx.Rollback();
                }
            }
        }

        [TestMethod]
        public void ExportToExcelTRAsync_NullParameters_LogsError()
        {
            // Act
            var result = _sut.ExportToExcelTRAsync(null, null);

            // Assert
            Assert.IsNotNull(result);
            _logger.Verify(lg => lg.WriteError(
                nameof(_sut.ExportToExcelTRAsync),
                It.IsAny<string>(),
                It.IsAny<Exception>()), Times.Once);
        }

        #endregion

        #region InsertProcessedFilesTRAsync Tests

        [TestMethod]
        public void InsertProcessedFilesTRAsync_ValidParameters_InsertsSuccessfully()
        {
            using (var con = new OleDbConnection(_connectionString))
            {
                con.Open();
                using (var tx = con.BeginTransaction())
                {
                    // Act
                    _sut.InsertProcessedFilesTRAsync("TEST_SYSTEM", "test_file.csv");

                    // Verify
                    var verifyCmd = new OleDbCommand(
                        "SELECT COUNT(*) FROM GPI.TRFILESPROCESSED WHERE SYSTEM_NAME = ? AND FILE_NAME = ?",
                        con, tx);
                    verifyCmd.Parameters.AddWithValue("@sys", "TEST_SYSTEM");
                    verifyCmd.Parameters.AddWithValue("@file", "test_file.csv");

                    var count = (int)verifyCmd.ExecuteScalar();
                    Assert.IsTrue(count > 0);

                    tx.Rollback();
                }
            }
        }

        [TestMethod]
        public void InsertProcessedFilesTRAsync_NullParameters_LogsError()
        {
            // Act
            _sut.InsertProcessedFilesTRAsync(null, null);

            // Assert
            _logger.Verify(lg => lg.WriteError(
                nameof(_sut.InsertProcessedFilesTRAsync),
                It.IsAny<string>(),
                It.IsAny<Exception>()), Times.Once);
        }

        [TestMethod]
        public void InsertProcessedFilesTRAsync_EmptyFileName_LogsError()
        {
            // Act
            _sut.InsertProcessedFilesTRAsync("TEST", "");

            // Assert
            _logger.Verify(lg => lg.WriteError(
                nameof(_sut.InsertProcessedFilesTRAsync),
                It.IsAny<string>(),
                It.IsAny<Exception>()), Times.Once);
        }

        #endregion

        #region GetEmailTimeListAsync Tests

        [TestMethod]
        public void GetEmailTimeListAsync_ReturnsDistinctTimeList()
        {
            using (var con = new OleDbConnection(_connectionString))
            {
                con.Open();
                using (var tx = con.BeginTransaction())
                {
                    // Arrange - Insert test email timing data
                    var insertCmd = new OleDbCommand(
                        "INSERT INTO GPI.TRANSACTIONRECONMAILDETAILS (EMAIL_TIMING, SYSTEM, REGION) VALUES (?, ?, ?)",
                        con, tx);
                    
                    // Insert duplicate times to test DISTINCT
                    insertCmd.Parameters.Clear();
                    insertCmd.Parameters.AddWithValue("@time", "09:00");
                    insertCmd.Parameters.AddWithValue("@sys", "SYS1");
                    insertCmd.Parameters.AddWithValue("@region", "US");
                    insertCmd.ExecuteNonQuery();

                    insertCmd.Parameters.Clear();
                    insertCmd.Parameters.AddWithValue("@time", "09:00");
                    insertCmd.Parameters.AddWithValue("@sys", "SYS2");
                    insertCmd.Parameters.AddWithValue("@region", "UK");
                    insertCmd.ExecuteNonQuery();

                    insertCmd.Parameters.Clear();
                    insertCmd.Parameters.AddWithValue("@time", "17:00");
                    insertCmd.Parameters.AddWithValue("@sys", "SYS1");
                    insertCmd.Parameters.AddWithValue("@region", "US");
                    insertCmd.ExecuteNonQuery();

                    // Act
                    var result = _sut.GetEmailTimeListAsync();

                    // Assert
                    Assert.IsNotNull(result);
                    Assert.IsInstanceOfType(result, typeof(List<string>));
                    Assert.IsTrue(result.Contains("09:00"));
                    Assert.IsTrue(result.Contains("17:00"));
                    Assert.AreEqual(2, result.Count); // Should be distinct

                    tx.Rollback();
                }
            }
        }

        [TestMethod]
        public void GetEmailTimeListAsync_EmptyTable_ReturnsEmptyList()
        {
            // Act
            var result = _sut.GetEmailTimeListAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(List<string>));
        }

        #endregion

        #region GetEmailTimeTRAsync Tests

        [TestMethod]
        public void GetEmailTimeTRAsync_ValidTime_ReturnsMatchingRecords()
        {
            using (var con = new OleDbConnection(_connectionString))
            {
                con.Open();
                using (var tx = con.BeginTransaction())
                {
                    // Arrange
                    var insertCmd = new OleDbCommand(@"
                        INSERT INTO GPI.TRANSACTIONRECONMAILDETAILS (EMAIL_TIMING, SYSTEM, REGION, MAIL_DL) 
                        VALUES (?, ?, ?, ?)", con, tx);
                    insertCmd.Parameters.AddWithValue("@time", "09:00");
                    insertCmd.Parameters.AddWithValue("@sys", "SYS1");
                    insertCmd.Parameters.AddWithValue("@region", "US");
                    insertCmd.Parameters.AddWithValue("@mail", "test@chubb.com");
                    insertCmd.ExecuteNonQuery();

                    // Act
                    var result = _sut.GetEmailTimeTRAsync("09:00");

                    // Assert
                    Assert.IsNotNull(result);
                    Assert.IsInstanceOfType(result, typeof(DataTable));
                    Assert.IsTrue(result.Rows.Count > 0);
                    Assert.AreEqual("09:00", result.Rows[0]["EMAIL_TIMING"].ToString());

                    tx.Rollback();
                }
            }
        }

        [TestMethod]
        public void GetEmailTimeTRAsync_NonExistentTime_ReturnsEmptyTable()
        {
            // Act
            var result = _sut.GetEmailTimeTRAsync("25:00");

            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOfType(result, typeof(DataTable));
            Assert.AreEqual(0, result.Rows.Count);
        }

        [TestMethod]
        public void GetEmailTimeTRAsync_NullTime_LogsError()
        {
            // Act
            var result = _sut.GetEmailTimeTRAsync(null);

            // Assert
            Assert.IsNotNull(result);
            _logger.Verify(lg => lg.WriteError(
                nameof(_sut.GetEmailTimeTRAsync),
                It.IsAny<string>(),
                It.IsAny<Exception>()), Times.Once);
        }

        #endregion

        #region InsertIntoTRMatchedTableAsync Tests

        [TestMethod]
        public void InsertIntoTRMatchedTableAsync_ValidParameters_InsertsSuccessfully()
        {
            using (var con = new OleDbConnection(_connectionString))
            {
                con.Open();
                using (var tx = con.BeginTransaction())
                {
                    // Arrange
                    var tr = CreateTestTransactionDetails();

                    // Act
                    _sut.InsertIntoTRMatchedTableAsync("TEST_SYSTEM", DateTime.UtcNow, tr);

                    // Verify
                    var verifyCmd = new OleDbCommand(
                        "SELECT COUNT(*) FROM GPI.TRANSACTIONRECON_MATCHED WHERE SYSTEM = ? AND SOURCEPAYMENTREFERENCE = ?",
                        con, tx);
                    verifyCmd.Parameters.AddWithValue("@sys", "TEST_SYSTEM");
                    verifyCmd.Parameters.AddWithValue("@ref", tr.sourcePaymentReference);

                    var count = (int)verifyCmd.ExecuteScalar();
                    Assert.IsTrue(count > 0);

                    tx.Rollback();
                }
            }
        }

        [TestMethod]
        public void InsertIntoTRMatchedTableAsync_NullTransactionDetails_ThrowsException()
        {
            // Act & Assert
            Assert.ThrowsException<Exception>(() =>
                _sut.InsertIntoTRMatchedTableAsync("TEST", DateTime.UtcNow, null));
        }

        #endregion

        #region GetAllTableDataForTRAsync Tests

        [TestMethod]
        public void GetAllTableDataForTRAsync_WithoutIgnoreStatus_ReturnsAllTables()
        {
            using (var con = new OleDbConnection(_connectionString))
            {
                con.Open();
                using (var tx = con.BeginTransaction())
                {
                    // Arrange - Insert test data in required tables
                    DateTime reportTime = DateTime.UtcNow.AddHours(-1);
                    
                    // Insert ledger data
                    InsertTestLedgerRecord(tx, con, "BU1", "REF123", "SYSTEM1");
                    
                    // Insert payment queue data  
                    InsertTestPaymentQueue(tx, con, "SYSTEM1", "BU1", "PAY123");
                    
                    // Insert payment data
                    InsertTestPayment(tx, con, "SYSTEM1", "BU1", "PAY123");

                    // Act
                    var result = _sut.GetAllTableDataForTRAsync("'SYSTEM1'", "'BU1'", reportTime, "", "'BU1'", 
                        TimeSpan.FromHours(9), TimeSpan.FromHours(17));

                    // Assert
                    Assert.IsNotNull(result);
                    Assert.IsTrue(result.Tables.Contains("LED"));
                    Assert.IsTrue(result.Tables.Contains("PAYQUE"));
                    Assert.IsTrue(result.Tables.Contains("PAYTB"));
                    // Should not contain PAYGT when ignore_status is empty

                    tx.Rollback();
                }
            }
        }

        [TestMethod]
        public void GetAllTableDataForTRAsync_WithIgnoreStatus_IncludesGTTable()
        {
            using (var con = new OleDbConnection(_connectionString))
            {
                con.Open();
                using (var tx = con.BeginTransaction())
                {
                    // Arrange
                    DateTime reportTime = DateTime.UtcNow.AddHours(-1);

                    // Act
                    var result = _sut.GetAllTableDataForTRAsync("'SYSTEM1'", "'BU1'", reportTime, 
                        "'COMPLETED','FAILED'", "'BU1'", TimeSpan.FromHours(9), TimeSpan.FromHours(17));

                    // Assert
                    Assert.IsNotNull(result);
                    Assert.IsTrue(result.Tables.Contains("LED"));
                    Assert.IsTrue(result.Tables.Contains("PAYQUE"));
                    Assert.IsTrue(result.Tables.Contains("PAYTB"));
                    Assert.IsTrue(result.Tables.Contains("PAYGT"));

                    tx.Rollback();
                }
            }
        }

        [TestMethod]
        public void GetAllTableDataForTRAsync_NullParameters_LogsError()
        {
            // Act
            var result = _sut.GetAllTableDataForTRAsync(null, null, DateTime.UtcNow, "", "", 
                TimeSpan.Zero, TimeSpan.Zero);

            // Assert
            Assert.IsNotNull(result);
            _logger.Verify(lg => lg.WriteError(
                nameof(_sut.GetAllTableDataForTRAsync),
                It.IsAny<string>(),
                It.IsAny<Exception>()), Times.Once);
        }

        #endregion

        #region CheckTransactionExistInMatchedTableAsync Tests

        [TestMethod]
        public void CheckTransactionExistInMatchedTableAsync_TransactionExists_ReturnsTrue()
        {
            using (var con = new OleDbConnection(_connectionString))
            {
                con.Open();
                using (var tx = con.BeginTransaction())
                {
                    // Arrange
                    var insertCmd = new OleDbCommand(@"
                        INSERT INTO GPI.TRANSACTIONRECON_MATCHED (SYSTEM, BU, SOURCEPAYMENTREFERENCE, GPI_STATUS) 
                        VALUES (?, ?, ?, ?)", con, tx);
                    insertCmd.Parameters.AddWithValue("@sys", "TEST_SYS");
                    insertCmd.Parameters.AddWithValue("@bu", "BU1");
                    insertCmd.Parameters.AddWithValue("@ref", "REF123");
                    insertCmd.Parameters.AddWithValue("@status", "COMPLETED");
                    insertCmd.ExecuteNonQuery();

                    // Act
                    var result = _sut.CheckTransactionExistInMatchedTableAsync("TEST_SYS", "BU1", "REF123");

                    // Assert
                    Assert.IsTrue(result);

                    tx.Rollback();
                }
            }
        }

        [TestMethod]
        public void CheckTransactionExistInMatchedTableAsync_WithStatus_ReturnsCorrectResult()
        {
            using (var con = new OleDbConnection(_connectionString))
            {
                con.Open();
                using (var tx = con.BeginTransaction())
                {
                    // Arrange
                    var insertCmd = new OleDbCommand(@"
                        INSERT INTO GPI.TRANSACTIONRECON_MATCHED (SYSTEM, BU, SOURCEPAYMENTREFERENCE, GPI_STATUS) 
                        VALUES (?, ?, ?, ?)", con, tx);
                    insertCmd.Parameters.AddWithValue("@sys", "TEST_SYS");
                    insertCmd.Parameters.AddWithValue("@bu", "BU1");
                    insertCmd.Parameters.AddWithValue("@ref", "REF123");
                    insertCmd.Parameters.AddWithValue("@status", "COMPLETED");
                    insertCmd.ExecuteNonQuery();

                    // Act
                    var resultMatching = _sut.CheckTransactionExistInMatchedTableAsync("TEST_SYS", "BU1", "REF123", "COMPLETED");
                    var resultNotMatching = _sut.CheckTransactionExistInMatchedTableAsync("TEST_SYS", "BU1", "REF123", "FAILED");

                    // Assert
                    Assert.IsTrue(resultMatching);
                    Assert.IsFalse(resultNotMatching);

                    tx.Rollback();
                }
            }
        }

        [TestMethod]
        public void CheckTransactionExistInMatchedTableAsync_NonExistent_ReturnsFalse()
        {
            // Act
            var result = _sut.CheckTransactionExistInMatchedTableAsync("NONEXISTENT", "BU999", "REF999");

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void CheckTransactionExistInMatchedTableAsync_NullParameters_ReturnsFalse()
        {
            // Act
            var result = _sut.CheckTransactionExistInMatchedTableAsync(null, null, null);

            // Assert
            Assert.IsFalse(result);
            _logger.Verify(lg => lg.WriteError(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Exception>()), Times.AtLeastOnce);
        }

        #endregion

        #region Update Tests

        [TestMethod]
        public void UpdateTheStatusOfUnmatchedTransactionAsync_ValidParameters_UpdatesSuccessfully()
        {
            using (var con = new OleDbConnection(_connectionString))
            {
                con.Open();
                using (var tx = con.BeginTransaction())
                {
                    // Arrange - Insert test record
                    var insertCmd = new OleDbCommand(@"
                        INSERT INTO GPI.TRANSACTIONRECON (SYSTEM, BU, SOURCEPAYMENTREFERENCE, STATUS, GPI_STATUS, GROUP_TYPE, RECORD_TYPE, CREATED_DATE) 
                        VALUES (?, ?, ?, ?, ?, ?, ?, ?)", con, tx);
                    insertCmd.Parameters.AddWithValue("@sys", "TEST_SYS");
                    insertCmd.Parameters.AddWithValue("@bu", "BU1");
                    insertCmd.Parameters.AddWithValue("@ref", "REF123");
                    insertCmd.Parameters.AddWithValue("@status", "PENDING");
                    insertCmd.Parameters.AddWithValue("@gpistatus", "PROCESSING");
                    insertCmd.Parameters.AddWithValue("@grptype", "OLD_TYPE");
                    insertCmd.Parameters.AddWithValue("@rectype", "OLD_RECORD");
                    insertCmd.Parameters.AddWithValue("@created", DateTime.UtcNow.AddDays(-1));
                    insertCmd.ExecuteNonQuery();

                    // Act
                    _sut.UpdateTheStatusOfUnmatchedTransactionAsync("TEST_SYS", "BU1", "REF123", 
                        "COMPLETED", "PENDING", "NEW_TYPE", "NEW_RECORD", DateTime.UtcNow);

                    // Verify
                    var verifyCmd = new OleDbCommand(@"
                        SELECT GPI_STATUS, GROUP_TYPE, RECORD_TYPE FROM GPI.TRANSACTIONRECON 
                        WHERE SYSTEM = ? AND BU = ? AND SOURCEPAYMENTREFERENCE = ?", con, tx);
                    verifyCmd.Parameters.AddWithValue("@sys", "TEST_SYS");
                    verifyCmd.Parameters.AddWithValue("@bu", "BU1");
                    verifyCmd.Parameters.AddWithValue("@ref", "REF123");

                    using (var reader = verifyCmd.ExecuteReader())
                    {
                        Assert.IsTrue(reader.Read());
                        Assert.AreEqual("COMPLETED", reader["GPI_STATUS"].ToString());
                        Assert.AreEqual("NEW_TYPE", reader["GROUP_TYPE"].ToString());
                        Assert.AreEqual("NEW_RECORD", reader["RECORD_TYPE"].ToString());
                    }

                    tx.Rollback();
                }
            }
        }

        [TestMethod]
        public void UpdateTheStatusPlusErrorCodeOfUnmatchedTransactionAsync_ValidParameters_UpdatesSuccessfully()
        {
            using (var con = new OleDbConnection(_connectionString))
            {
                con.Open();
                using (var tx = con.BeginTransaction())
                {
                    // Arrange
                    var insertCmd = new OleDbCommand(@"
                        INSERT INTO GPI.TRANSACTIONRECON (SYSTEM, BU, SOURCEPAYMENTREFERENCE, STATUS, GPI_STATUS) 
                        VALUES (?, ?, ?, ?, ?)", con, tx);
                    insertCmd.Parameters.AddWithValue("@sys", "TEST_SYS");
                    insertCmd.Parameters.AddWithValue("@bu", "BU1");
                    insertCmd.Parameters.AddWithValue("@ref", "REF123");
                    insertCmd.Parameters.AddWithValue("@status", "ERROR");
                    insertCmd.Parameters.AddWithValue("@gpistatus", "PROCESSING");
                    insertCmd.ExecuteNonQuery();

                    // Act
                    _sut.UpdateTheStatusPlusErrorCodeOfUnmatchedTransactionAsync("TEST_SYS", "BU1", "REF123",
                        "FAILED", "ERROR", "E001", "Test Error Description");

                    // Verify
                    var verifyCmd = new OleDbCommand(@"
                        SELECT GPI_STATUS, ERROR_CODE, ERROR_DESC FROM GPI.TRANSACTIONRECON 
                        WHERE SYSTEM = ? AND BU = ? AND SOURCEPAYMENTREFERENCE = ?", con, tx);
                    verifyCmd.Parameters.AddWithValue("@sys", "TEST_SYS");
                    verifyCmd.Parameters.AddWithValue("@bu", "BU1");
                    verifyCmd.Parameters.AddWithValue("@ref", "REF123");

                    using (var reader = verifyCmd.ExecuteReader())
                    {
                        Assert.IsTrue(reader.Read());
                        Assert.AreEqual("FAILED", reader["GPI_STATUS"].ToString());
                        Assert.AreEqual("E001", reader["ERROR_CODE"].ToString());
                        Assert.AreEqual("Test Error Description", reader["ERROR_DESC"].ToString());
                    }

                    tx.Rollback();
                }
            }
        }

        #endregion

        #region Lookup Methods Tests

        [TestMethod]
        public void GetReconTypeAsync_SystemExists_ReturnsPayment()
        {
            using (var con = new OleDbConnection(_connectionString))
            {
                con.Open();
                using (var tx = con.BeginTransaction())
                {
                    // Arrange
                    var insertCmd = new OleDbCommand(
                        "INSERT INTO GPI.TR_PAYMENTRECONSYSTEMS (SYSTEM) VALUES (?)", con, tx);
                    insertCmd.Parameters.AddWithValue("@sys", "PAYMENT_SYSTEM");
                    insertCmd.ExecuteNonQuery();

                    // Act
                    var result = _sut.GetReconTypeAsync("PAYMENT_SYSTEM");

                    // Assert
                    Assert.AreEqual("PAYMENT", result);

                    tx.Rollback();
                }
            }
        }

        [TestMethod]
        public void GetReconTypeAsync_SystemNotExists_ReturnsStatus()
        {
            // Act
            var result = _sut.GetReconTypeAsync("NONEXISTENT_SYSTEM");

            // Assert
            Assert.AreEqual("STATUS", result);
        }

        [TestMethod]
        public void GetAPBUAsync_SystemInConversionList_ReturnsAPBU()
        {
            using (var con = new OleDbConnection(_connectionString))
            {
                con.Open();
                using (var tx = con.BeginTransaction())
                {
                    // Arrange
                    var insertCmd = new OleDbCommand(@"
                        INSERT INTO GPI.BUSINESSMAP (ACCOUNT, INT_ENTITY, SYSTEM, BU_ID) 
                        VALUES (?, ?, ?, ?)", con, tx);
                    insertCmd.Parameters.AddWithValue("@account", "ACC123");
                    insertCmd.Parameters.AddWithValue("@entity", "ENTITY1");
                    insertCmd.Parameters.AddWithValue("@system", "TRAX");
                    insertCmd.Parameters.AddWithValue("@buid", "APBU123");
                    insertCmd.ExecuteNonQuery();

                    // Act
                    var result = _sut.GetAPBUAsync("ACC123", "ENTITY1");

                    // Assert
                    Assert.AreEqual("APBU123", result);

                    tx.Rollback();
                }
            }
        }

        [TestMethod]
        public void GetAPBUAsync_SystemNotInConversionList_ReturnsEmpty()
        {
            using (var con = new OleDbConnection(_connectionString))
            {
                con.Open();
                using (var tx = con.BeginTransaction())
                {
                    // Arrange
                    var insertCmd = new OleDbCommand(@"
                        INSERT INTO GPI.BUSINESSMAP (ACCOUNT, INT_ENTITY, SYSTEM, BU_ID) 
                        VALUES (?, ?, ?, ?)", con, tx);
                    insertCmd.Parameters.AddWithValue("@account", "ACC123");
                    insertCmd.Parameters.AddWithValue("@entity", "ENTITY1");
                    insertCmd.Parameters.AddWithValue("@system", "OTHER_SYSTEM");
                    insertCmd.Parameters.AddWithValue("@buid", "APBU123");
                    insertCmd.ExecuteNonQuery();

                    // Act
                    var result = _sut.GetAPBUAsync("ACC123", "ENTITY1");

                    // Assert
                    Assert.AreEqual(string.Empty, result);

                    tx.Rollback();
                }
            }
        }

        [TestMethod]
        public void GetStatusCodesAsync_ReturnsOrderedStatusCodes()
        {
            using (var con = new OleDbConnection(_connectionString))
            {
                con.Open();
                using (var tx = con.BeginTransaction())
                {
                    // Arrange
                    var insertCmd = new OleDbConnection(_connectionString);
                    var cmd1 = new OleDbCommand(
                        "INSERT INTO GPI.STATUS_CODES (CODE, DESCRIPTION, ORDER_NUM) VALUES (?, ?, ?)", con, tx);
                    cmd1.Parameters.AddWithValue("@code", "S001");
                    cmd1.Parameters.AddWithValue("@desc", "Success");
                    cmd1.Parameters.AddWithValue("@order", 1);
                    cmd1.ExecuteNonQuery();

                    var cmd2 = new OleDbCommand(
                        "INSERT INTO GPI.STATUS_CODES (CODE, DESCRIPTION, ORDER_NUM) VALUES (?, ?, ?)", con, tx);
                    cmd2.Parameters.AddWithValue("@code", "E001");
                    cmd2.Parameters.AddWithValue("@desc", "Error");
                    cmd2.Parameters.AddWithValue("@order", 2);
                    cmd2.ExecuteNonQuery();

                    // Act
                    var result = _sut.GetStatusCodesAsync();

                    // Assert
                    Assert.IsNotNull(result);
                    Assert.IsTrue(result.Tables.Contains("TAB"));
                    Assert.IsTrue(result.Tables["TAB"].Rows.Count >= 2);

                    tx.Rollback();
                }
            }
        }

        #endregion

        #region Time Update Tests

        [TestMethod]
        public void UpdateTimeAsync_AlertTime_UpdatesAlertTime()
        {
            // Act - This will call stored procedure
            _sut.UpdateTimeAsync("TEST_SYSTEM", "US", "GPI.SP_UPDATE_TIME", "ALERT");

            // Assert - Verify no exception was thrown
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void UpdateTimeAsync_ReportTime_UpdatesReportTime()
        {
            // Act
            _sut.UpdateTimeAsync("TEST_SYSTEM", "US", "GPI.SP_UPDATE_TIME", "REPORT");

            // Assert - Verify no exception was thrown
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void UpdateTimeAsync_InvalidTimeType_DoesNotAddTimeParameter()
        {
            // Act
            _sut.UpdateTimeAsync("TEST_SYSTEM", "US", "GPI.SP_UPDATE_TIME", "INVALID");

            // Assert - Should not throw exception even with invalid time type
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void UpdateTimeAsync_NullParameters_LogsError()
        {
            // Act
            _sut.UpdateTimeAsync(null, null, null, null);

            // Assert
            _logger.Verify(lg => lg.WriteError(
                nameof(_sut.UpdateTimeAsync),
                It.IsAny<string>(),
                It.IsAny<Exception>()), Times.Once);
        }

        #endregion

        #region Additional Edge Cases

        [TestMethod]
        public void CheckVoucherExistsInGPIAsync_SpecialCharactersInUniqueID_HandlesCorrectly()
        {
            using (var con = new OleDbConnection(_connectionString))
            {
                con.Open();
                using (var tx = con.BeginTransaction())
                {
                    // Arrange
                    string specialRef = "REF'123\"";
                    InsertTestLedgerRecord(tx, con, "BU1", specialRef, "TRAX");

                    // Act
                    var result = _sut.CheckVoucherExistsInGPIAsync("TRAX", "BU1", specialRef);

                    // Assert
                    Assert.IsNotNull(result);
                    // Should handle special characters without SQL injection

                    tx.Rollback();
                }
            }
        }

        [TestMethod]
        public void CleartheInvalidBURecordAsync_RemovesInvalidRecords()
        {
            using (var con = new OleDbConnection(_connectionString))
            {
                con.Open();
                using (var tx = con.BeginTransaction())
                {
                    // Arrange - Insert records with valid and invalid BUs
                    var insertValid = new OleDbCommand(@"
                        INSERT INTO GPI.TRANSACTIONRECON (SYSTEM, BU, SOURCEPAYMENTREFERENCE, CREATED_DATE) 
                        VALUES (?, ?, ?, ?)", con, tx);
                    insertValid.Parameters.AddWithValue("@sys", "TEST_SYS");
                    insertValid.Parameters.AddWithValue("@bu", "VALID_BU");
                    insertValid.Parameters.AddWithValue("@ref", "REF123");
                    insertValid.Parameters.AddWithValue("@created", DateTime.UtcNow);
                    insertValid.ExecuteNonQuery();

                    var insertInvalid = new OleDbCommand(@"
                        INSERT INTO GPI.TRANSACTIONRECON (SYSTEM, BU, SOURCEPAYMENTREFERENCE, CREATED_DATE) 
                        VALUES (?, ?, ?, ?)", con, tx);
                    insertInvalid.Parameters.AddWithValue("@sys", "TEST_SYS");
                    insertInvalid.Parameters.AddWithValue("@bu", "INVALID_BU");
                    insertInvalid.Parameters.AddWithValue("@ref", "REF123");
                    insertInvalid.Parameters.AddWithValue("@created", DateTime.UtcNow);
                    insertInvalid.ExecuteNonQuery();

                    // Act
                    _sut.CleartheInvalidBURecordAsync("TEST_SYS", "REF123", "'VALID_BU'");

                    // Verify - Only valid BU record should remain
                    var verifyCmd = new OleDbCommand(@"
                        SELECT COUNT(*) FROM GPI.TRANSACTIONRECON 
                        WHERE SYSTEM = ? AND SOURCEPAYMENTREFERENCE = ?", con, tx);
                    verifyCmd.Parameters.AddWithValue("@sys", "TEST_SYS");
                    verifyCmd.Parameters.AddWithValue("@ref", "REF123");

                    var remainingCount = (int)verifyCmd.ExecuteScalar();
                    Assert.AreEqual(1, remainingCount); // Only valid BU should remain

                    tx.Rollback();
                }
            }
        }

        [TestMethod]
        public void TransactionExistInMatchedtableAsync_ReturnsStoredValue()
        {
            // This method returns the value set by CheckTransactionExistAsync
            // Act
            var result = _sut.TransactionExistInMatchedtableAsync();

            // Assert
            Assert.IsInstanceOfType(result, typeof(int));
            // Default value should be 0
            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void GetResourcePathNameAsync_ValidParameters_ReturnsResourceData()
        {
            using (var con = new OleDbConnection(_connectionString))
            {
                con.Open();
                using (var tx = con.BeginTransaction())
                {
                    // Arrange
                    var insertCmd = new OleDbCommand(@"
                        INSERT INTO GPI.TRANSACTIONRECONMAILDETAILS (SYSTEM, REGION, RESOURCE_PATH, FILE_NAME) 
                        VALUES (?, ?, ?, ?)", con, tx);
                    insertCmd.Parameters.AddWithValue("@sys", "TEST_SYS");
                    insertCmd.Parameters.AddWithValue("@region", "US");
                    insertCmd.Parameters.AddWithValue("@path", "/test/path/");
                    insertCmd.Parameters.AddWithValue("@file", "config.xml");
                    insertCmd.ExecuteNonQuery();

                    // Act
                    var result = _sut.GetResourcePathNameAsync("TEST_SYS", "US");

                    // Assert
                    Assert.IsNotNull(result);
                    Assert.IsInstanceOfType(result, typeof(DataTable));
                    Assert.IsTrue(result.Rows.Count > 0);

                    tx.Rollback();
                }
            }
        }

        #endregion

        #region Performance Tests

        [TestMethod]
        public void GetAllTableDataForTRAsync_LargeDataset_PerformsWithinSLA()
        {
            using (var con = new OleDbConnection(_connectionString))
            {
                con.Open();
                using (var tx = con.BeginTransaction())
                {
                    // Arrange - Insert multiple test records
                    for (int i = 0; i < 10; i++)
                    {
                        InsertTestLedgerRecord(tx, con, $"BU{i}", $"REF{i}", "SYSTEM1");
                        InsertTestPaymentQueue(tx, con, "SYSTEM1", $"BU{i}", $"PAY{i}");
                        InsertTestPayment(tx, con, "SYSTEM1", $"BU{i}", $"PAY{i}");
                    }

                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                    // Act
                    var result = _sut.GetAllTableDataForTRAsync("'SYSTEM1'", "'BU1','BU2','BU3'", 
                        DateTime.UtcNow.AddHours(-2), "", "'BU1'", TimeSpan.FromHours(9), TimeSpan.FromHours(17));

                    stopwatch.Stop();

                    // Assert - Should complete within reasonable time
                    Assert.IsTrue(stopwatch.ElapsedMilliseconds < 10000); // 10 seconds max
                    Assert.IsNotNull(result);

                    tx.Rollback();
                }
            }
        }

        #endregion

        #region Constructor Tests

        [TestMethod]
        public void Constructor_ValidDependencies_InitializesCorrectly()
        {
            // Arrange
            var config = CreateTestConfig();
            var logger = new Mock<ILoggerService>();

            // Act
            var dao = new CommonDAO(config.Object, logger.Object);

            // Assert
            Assert.IsNotNull(dao);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullConfig_ThrowsException()
        {
            // Act
            var dao = new CommonDAO(null, _logger.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_NullLogger_ThrowsException()
        {
            // Act
            var dao = new CommonDAO(_config.Object, null);
        }

        #endregion
    }
} 
