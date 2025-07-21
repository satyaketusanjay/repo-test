using Microsoft.VisualStudio.TestTools.UnitTesting;
using GPI.BusinessEntities;
using TransactionRecon.Tests.Helpers;

namespace TransactionRecon.Tests.Tests
{
    [TestClass]
    public class ConstantsTests
    {
        #region Constants Tests

        [TestMethod]
        public void Constants_EODRResource_ShouldReturnCorrectValue()
        {
            // Arrange & Act
            var result = Constants.EODRRESOURCE;

            // Assert
            Assert.AreEqual("/EODRProcess", result);
        }

        [TestMethod]
        public void Constants_TraxSystem_ShouldReturnCorrectValue()
        {
            // Arrange & Act
            var result = Constants.TraxSystem;

            // Assert
            Assert.AreEqual("TRAX", result);
        }

        [TestMethod]
        public void Constants_VoidRejectionStatus_ShouldReturnCorrectValue()
        {
            // Arrange & Act
            var result = Constants.VoidRejectionStatus;

            // Assert
            Assert.AreEqual("EVD", result);
        }

        [TestMethod]
        public void Constants_RejectionStatus_ShouldReturnCorrectValue()
        {
            // Arrange & Act
            var result = Constants.RejectionStatus;

            // Assert
            Assert.AreEqual("FRJ", result);
        }

        [TestMethod]
        public void Constants_StatusFRJ_ShouldReturnCorrectValue()
        {
            // Arrange & Act
            var result = Constants.StatusFRJ;

            // Assert
            Assert.AreEqual("FRJ", result);
        }

        [TestMethod]
        public void Constants_StatusFOD_ShouldReturnCorrectValue()
        {
            // Arrange & Act
            var result = Constants.StatusFOD;

            // Assert
            Assert.AreEqual("FOD", result);
        }

        [TestMethod]
        public void Constants_StatusFST_ShouldReturnCorrectValue()
        {
            // Arrange & Act
            var result = Constants.StatusFST;

            // Assert
            Assert.AreEqual("FST", result);
        }

        [TestMethod]
        public void Constants_StatusFRE_ShouldReturnCorrectValue()
        {
            // Arrange & Act
            var result = Constants.StatusFRE;

            // Assert
            Assert.AreEqual("FRE", result);
        }

        [TestMethod]
        public void Constants_StatusFPD_ShouldReturnCorrectValue()
        {
            // Arrange & Act
            var result = Constants.StatusFPD;

            // Assert
            Assert.AreEqual("FPD", result);
        }

        [TestMethod]
        public void Constants_StatusFSL_ShouldReturnCorrectValue()
        {
            // Arrange & Act
            var result = Constants.StatusFSL;

            // Assert
            Assert.AreEqual("FSL", result);
        }

        [TestMethod]
        public void Constants_StatusGPIFCP_ShouldReturnCorrectValue()
        {
            // Arrange & Act
            var result = Constants.StatusGPIFCP;

            // Assert
            Assert.AreEqual("FCP", result);
        }

        [TestMethod]
        public void Constants_StatusGPIEVD_ShouldReturnCorrectValue()
        {
            // Arrange & Act
            var result = Constants.StatusGPIEVD;

            // Assert
            Assert.AreEqual("EVD", result);
        }

        [TestMethod]
        public void Constants_MemoPayment_ShouldReturnCorrectValue()
        {
            // Arrange & Act
            var result = Constants.MemoPayment;

            // Assert
            Assert.AreEqual("M", result);
        }

        [TestMethod]
        public void Constants_ESCDaily_ShouldReturnCorrectValue()
        {
            // Arrange & Act
            var result = Constants.ESCDaily;

            // Assert
            Assert.AreEqual("D", result);
        }

        [TestMethod]
        public void Constants_ESCWeekly_ShouldReturnCorrectValue()
        {
            // Arrange & Act
            var result = Constants.ESCWeekly;

            // Assert
            Assert.AreEqual("W", result);
        }

        [TestMethod]
        public void Constants_ESCMonthly_ShouldReturnCorrectValue()
        {
            // Arrange & Act
            var result = Constants.ESCMonthly;

            // Assert
            Assert.AreEqual("M", result);
        }

        [TestMethod]
        public void Constants_AlertTime_ShouldReturnCorrectValue()
        {
            // Arrange & Act
            var result = Constants.AlertTime;

            // Assert
            Assert.AreEqual("ALERT", result);
        }

        [TestMethod]
        public void Constants_ReportTime_ShouldReturnCorrectValue()
        {
            // Arrange & Act
            var result = Constants.ReportTime;

            // Assert
            Assert.AreEqual("REPORT", result);
        }

        [TestMethod]
        public void Constants_True_ShouldReturnCorrectValue()
        {
            // Arrange & Act
            var result = Constants.True;

            // Assert
            Assert.AreEqual("Y", result);
        }

        #endregion

        #region ErrorClass Tests

        [TestMethod]
        public void ErrorClass_Constructor_ShouldCreateInstance()
        {
            // Arrange & Act
            var errorClass = new ErrorClass();

            // Assert
            Assert.IsNotNull(errorClass);
        }

        [TestMethod]
        public void ErrorClass_RecordNo_CanBeSetAndRetrieved()
        {
            // Arrange
            var errorClass = new ErrorClass();
            const int expectedRecordNo = 123;

            // Act
            errorClass.RecordNo = expectedRecordNo;

            // Assert
            Assert.AreEqual(expectedRecordNo, errorClass.RecordNo);
        }

        [TestMethod]
        public void ErrorClass_ErrorDesc_CanBeSetAndRetrieved()
        {
            // Arrange
            var errorClass = new ErrorClass();
            const string expectedErrorDesc = "Test error description";

            // Act
            errorClass.ErrorDesc = expectedErrorDesc;

            // Assert
            Assert.AreEqual(expectedErrorDesc, errorClass.ErrorDesc);
        }

        [TestMethod]
        public void ErrorClass_DefaultValues_ShouldBeCorrect()
        {
            // Arrange & Act
            var errorClass = new ErrorClass();

            // Assert
            Assert.AreEqual(0, errorClass.RecordNo);
            Assert.IsNull(errorClass.ErrorDesc);
        }

        [TestMethod]
        public void ErrorClass_WithValidData_ShouldCreateCorrectInstance()
        {
            // Arrange
            var testData = TestDataHelper.CreateValidErrorClass();

            // Act & Assert
            Assert.AreEqual(1, testData.RecordNo);
            Assert.AreEqual("Test error description", testData.ErrorDesc);
        }

        [TestMethod]
        public void ErrorClass_WithInvalidData_ShouldHandleNegativeValues()
        {
            // Arrange
            var testData = TestDataHelper.CreateInvalidErrorClass();

            // Act & Assert
            Assert.AreEqual(-1, testData.RecordNo);
            Assert.IsNull(testData.ErrorDesc);
        }

        [TestMethod]
        public void ErrorClass_ErrorDesc_CanBeNull()
        {
            // Arrange
            var errorClass = new ErrorClass();

            // Act
            errorClass.ErrorDesc = null;

            // Assert
            Assert.IsNull(errorClass.ErrorDesc);
        }

        [TestMethod]
        public void ErrorClass_ErrorDesc_CanBeEmptyString()
        {
            // Arrange
            var errorClass = new ErrorClass();

            // Act
            errorClass.ErrorDesc = string.Empty;

            // Assert
            Assert.AreEqual(string.Empty, errorClass.ErrorDesc);
        }

        [TestMethod]
        public void ErrorClass_RecordNo_CanBeZero()
        {
            // Arrange
            var errorClass = new ErrorClass();

            // Act
            errorClass.RecordNo = 0;

            // Assert
            Assert.AreEqual(0, errorClass.RecordNo);
        }

        [TestMethod]
        public void ErrorClass_RecordNo_CanBeNegative()
        {
            // Arrange
            var errorClass = new ErrorClass();

            // Act
            errorClass.RecordNo = -1;

            // Assert
            Assert.AreEqual(-1, errorClass.RecordNo);
        }

        [TestMethod]
        public void ErrorClass_LargeRecordNo_ShouldBeHandled()
        {
            // Arrange
            var errorClass = new ErrorClass();
            const int largeValue = int.MaxValue;

            // Act
            errorClass.RecordNo = largeValue;

            // Assert
            Assert.AreEqual(largeValue, errorClass.RecordNo);
        }

        [TestMethod]
        public void ErrorClass_LongErrorDescription_ShouldBeHandled()
        {
            // Arrange
            var errorClass = new ErrorClass();
            var longDescription = new string('A', 1000);

            // Act
            errorClass.ErrorDesc = longDescription;

            // Assert
            Assert.AreEqual(longDescription, errorClass.ErrorDesc);
        }

        #endregion
    }
}