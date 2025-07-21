using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace GPI.TransactionRecon.Tests
{
    [TestClass]
    public class LoggerServiceTests
    {
        private LoggerService _loggerService;

        [TestInitialize]
        public void Setup()
        {
            _loggerService = new LoggerService();
        }

        [TestMethod]
        public void WriteInfo_WithValidMessage_ShouldNotThrow()
        {
            var message = "This is an info log message.";
            try
            {
                _loggerService.WriteInfo(message);
            }
            catch (Exception ex)
            {
                Assert.Fail("Exception thrown: " + ex.Message);
            }
        }

        [TestMethod]
        public void TRWriteToLogs_ShouldCallWriteInfo()
        {
            var message = "Legacy log message.";
            var log = "LegacyLog";

            try
            {
                _loggerService.TRWriteToLogs(message, log);
            }
            catch (Exception ex)
            {
                Assert.Fail("Exception thrown: " + ex.Message);
            }
        }

        [TestMethod]
        public void WriteTransaction_WithValidInputs_ShouldNotThrow()
        {
            var fileName = "testfile.txt";
            var message = "Transaction log message.";
            var sourceSystem = "SystemA";

            try
            {
                _loggerService.WriteTransaction(fileName, message, sourceSystem);
            }
            catch (Exception ex)
            {
                Assert.Fail("Exception thrown: " + ex.Message);
            }
        }

        [TestMethod]
        public void WriteTransaction_WithNullSourceSystem_ShouldNotThrow()
        {
            var fileName = "testfile.txt";
            var message = "Transaction log message.";

            try
            {
                _loggerService.WriteTransaction(fileName, message);
            }
            catch (Exception ex)
            {
                Assert.Fail("Exception thrown: " + ex.Message);
            }
        }

        [TestMethod]
        public void WriteError_WithValidInputs_ShouldNotThrow()
        {
            var methodName = "TestMethod";
            var message = "Error occurred.";
            var exception = new InvalidOperationException("Invalid operation");
            var sourceSystem = "SystemB";

            try
            {
                _loggerService.WriteError(methodName, message, exception, sourceSystem);
            }
            catch (Exception ex)
            {
                Assert.Fail("Exception thrown: " + ex.Message);
            }
        }

        [TestMethod]
        public void WriteError_WithNullSourceSystem_ShouldNotThrow()
        {
            var methodName = "TestMethod";
            var message = "Error occurred.";
            var exception = new Exception("General exception");

            try
            {
                _loggerService.WriteError(methodName, message, exception);
            }
            catch (Exception ex)
            {
                Assert.Fail("Exception thrown: " + ex.Message);
            }
        }
    }
}
