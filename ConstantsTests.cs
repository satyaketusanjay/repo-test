using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using GPI.TransactionRecon.BusinessLogic;

namespace GPI.TransactionRecon.BusinessLogic.Tests
{
    [TestClass]
    public class TRCommonTests
    {
        private TRCommon _common;

        [TestInitialize]
        public void Setup()
        {
            _common = new TRCommon();
        }

        // getGPITransactonObject Tests

        [TestMethod]
        public void getGPITransactonObject_Returns_ExpectedObject_AllFields()
        {
            var result = _common.getGPITransactonObject("sys","bu","acc","INR","ref",1000.1m,500.5m,"user","C","qual","COMP","ASIA",DateTime.Now,DateTime.Now,"uniqueID");
            Assert.IsNotNull(result);
            Assert.AreEqual("bu", result.businessUnit);
            Assert.AreEqual("ref", result.sourcePaymentReference);
            Assert.AreEqual(500.5m, result.AccountingAmount);
            Assert.AreEqual("COMP", result.statusGPI);
            Assert.AreEqual("uniqueID", result.uniquePaymentID);
            Assert.IsNotNull(result.trDateTime);
            Assert.IsNotNull(result.gpiModifiedTime);
        }

        [TestMethod]
        public void getGPITransactonObject_ModifiedDateDBNull_UsesCreatedDate()
        {
            var createdDate = DateTime.Now.AddHours(-1);
            var result = _common.getGPITransactonObject("sys","bu","acc","USD","ref",10m,20m,"c","V","q","COMP","NA",createdDate,DBNull.Value,"id");
            Assert.AreEqual(createdDate, result.gpiModifiedTime);
        }

        [TestMethod]
        public void getGPITransactonObject_ModifiedDateDBNull_CreatedDateDBNull_DefaultsToUtcNow()
        {
            var before = DateTime.UtcNow.AddSeconds(-1);
            var result = _common.getGPITransactonObject("sys","bu","acc","USD","ref",10m,20m,"c","F","q","OK","EU",DBNull.Value,DBNull.Value,"id");
            Assert.IsTrue(result.gpiModifiedTime >= before && result.gpiModifiedTime <= DateTime.UtcNow);
        }

        [TestMethod]
        public void getGPITransactonObject_Exception_Returns_Null()
        {
            // Simulates Cast exception by passing an invalid value
            var result = _common.getGPITransactonObject("sys","bu","acc","USD","ref",10m,20m,"c","G","q","OK","EU","wrongType","wrongType","id");
            Assert.IsNull(result);
        }

        // getFileName Tests

        [TestMethod]
        public void getFileName_Valid_ReturnsFormattedName()
        {
            string input = "FUND_SYS_ACC_CURR_FILENAME";
            string result = _common.getFileName(input);
            Assert.IsTrue(result.StartsWith("FUND_SYS_ACC_CURR_"));
        }

        [TestMethod]
        public void getFileName_EmptyInput_ReturnsEmpty()
        {
            Assert.AreEqual("", _common.getFileName(""));
        }

        // formResponseString Tests

        [TestMethod]
        public void formResponseString_ValidDataTable_ReturnsExpectedString()
        {
            DataTable dt = new DataTable();
            dt.Columns.AddRange(new[] {
                new DataColumn("RECONCILIATION DATE"),
                new DataColumn("SYSTEM"),
                new DataColumn("BU"),
                new DataColumn("SOURCEPAYMENTREFERENCE"),
                new DataColumn("LEDGER_ACCOUNT"),
                new DataColumn("CURRENCY"),
                new DataColumn("RECORD_TYPE"),
                new DataColumn("ORIGINAL_AMT"),
                new DataColumn("ACCOUNTING_AMT"),
                new DataColumn("CREATED_BY"),
                new DataColumn("GPI_STATUS"),
                new DataColumn("FES_STATUS"),
                new DataColumn("ERROR_CODE"),
                new DataColumn("ERROR_DESCRIPTION"),
                new DataColumn("LAST MODIFIED DATE"),
            });
            var row = dt.NewRow();
            row["RECONCILIATION DATE"] = "2025-07-21";
            row["SYSTEM"] = "GPI";
            row["BU"] = "BUX";
            row["SOURCEPAYMENTREFERENCE"] = "ref123";
            row["LEDGER_ACCOUNT"] = "1234";
            row["CURRENCY"] = "INR";
            row["RECORD_TYPE"] = "PAY";
            row["ORIGINAL_AMT"] = 10;
            row["ACCOUNTING_AMT"] = 5;
            row["CREATED_BY"] = "userA";
            row["GPI_STATUS"] = "COMPLETE";
            row["FES_STATUS"] = "";
            row["ERROR_CODE"] = "";
            row["ERROR_DESCRIPTION"] = "";
            row["LAST MODIFIED DATE"] = "2025-07-21 12:00";
            dt.Rows.Add(row);

            var str = _common.formResponseString(dt);
            Assert.IsTrue(str.Contains("ref123"));
            Assert.IsTrue(str.EndsWith(Environment.NewLine));
        }

        [TestMethod]
        public void formResponseString_EmptyTable_ReturnsEmptyString()
        {
            var dt = new DataTable();
            var str = _common.formResponseString(dt);
            Assert.AreEqual(string.Empty, str);
        }

        [TestMethod]
        public void formResponseString_DataWithCommas_QuotesField()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("RECONCILIATION DATE");
            dt.Columns.Add("SYSTEM");
            dt.Columns.Add("BU");
            dt.Columns.Add("SOURCEPAYMENTREFERENCE");
            dt.Columns.Add("LEDGER_ACCOUNT");
            dt.Columns.Add("CURRENCY");
            dt.Columns.Add("RECORD_TYPE");
            dt.Columns.Add("ORIGINAL_AMT");
            dt.Columns.Add("ACCOUNTING_AMT");
            dt.Columns.Add("CREATED_BY");
            dt.Columns.Add("GPI_STATUS");
            dt.Columns.Add("FES_STATUS");
            dt.Columns.Add("ERROR_CODE");
            dt.Columns.Add("ERROR_DESCRIPTION");
            dt.Columns.Add("LAST MODIFIED DATE");
            var row = dt.NewRow();
            row["RECONCILIATION DATE"] = "2025-07,21";
            dt.Rows.Add(row);

            var str = _common.formResponseString(dt);
            Assert.IsTrue(str.Contains("\"2025-07,21\""));
        }

        // getSqlsearchString Tests

        [TestMethod]
        public void getSqlsearchString_RemovesEmptyItems_BuildsExpectedQuery()
        {
            var items = new List<string> { "COMPLETE", "FAILED", "", "PENDING" };
            var result = _common.getSqlsearchString(items);
            Assert.AreEqual("'COMPLETE','FAILED','PENDING'", result);
        }

        [TestMethod]
        public void getSqlsearchString_EmptyList_ReturnsEmptyString()
        {
            var result = _common.getSqlsearchString(new List<string>());
            Assert.AreEqual(string.Empty, result);
        }

        // addRecordType Tests

        [TestMethod]
        public void addRecordType_GroupTypeC_SetsPAY()
        {
            var t = new TransactionReconcilationDetails { groupType = "C", qualifier = "qual" };
            var result = _common.addRecordType(t);
            Assert.AreEqual("PAY", result.recordType);
        }

        [TestMethod]
        public void addRecordType_GroupTypeF_SetsPAY()
        {
            var t = new TransactionReconcilationDetails { groupType = "F" };
            var result = _common.addRecordType(t);
            Assert.AreEqual("PAY", result.recordType);
        }

        [TestMethod]
        public void addRecordType_GroupTypeV_SetsMRR()
        {
            var t = new TransactionReconcilationDetails { groupType = "V" };
            var result = _common.addRecordType(t);
            Assert.AreEqual("MRR", result.recordType);
        }

        [TestMethod]
        public void addRecordType_GroupTypeOther_NullRecordType()
        {
            var t = new TransactionReconcilationDetails { groupType = "X" };
            var result = _common.addRecordType(t);
            Assert.IsNull(result.recordType);
        }

        [TestMethod]
        public void addRecordType_CatchesException_ReturnsSame()
        {
            var t = new TransactionReconcilationDetails();
            t.groupType = null;
            var result = _common.addRecordType(t);
            Assert.AreSame(t, result);
        }

        // getSqlsearch Tests

        [TestMethod]
        public void getSqlsearch_ValidString_ReturnsCommaSeparatedQuoted()
        {
            var result = _common.getSqlsearch("A,B,C");
            Assert.AreEqual("'A','B','C'", result);
        }

        [TestMethod]
        public void getSqlsearch_SingleValue_ReturnsSingleQuoted()
        {
            var result = _common.getSqlsearch("SINGLE");
            Assert.AreEqual("'SINGLE'", result);
        }

        [TestMethod]
        public void getSqlsearch_EmptyString_ReturnsEmptyQuotes()
        {
            var result = _common.getSqlsearch("");
            Assert.AreEqual("''", result);
        }
    }
}
