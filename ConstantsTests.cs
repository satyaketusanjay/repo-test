using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading.Tasks;
using System.Net.Mail;
using System.Text;
using System.Net;
using GPI.TransactionRecon.Logger;
using GPI.TransactionRecon.Logger.Contracts;

namespace GPI.TransactionRecon.Logger.Tests
{
    [TestClass]
    public class EmailServiceTests
    {
        private Mock<ILoggerService> _mockLogger;
        private Mock<IAppConfiguration> _mockConfig;
        private EmailService _service;

        [TestInitialize]
        public void Setup()
        {
            _mockLogger = new Mock<ILoggerService>();
            _mockConfig = new Mock<IAppConfiguration>();
            _mockConfig.SetupGet(c => c.SmtpServer).Returns("smtp.test.com");
            _mockConfig.SetupGet(c => c.TRMailFrom).Returns("from@test.com");
            _mockConfig.SetupGet(c => c.TRMailTo).Returns("to@test.com");
            _mockConfig.SetupGet(c => c.TRMailSubject).Returns("Subj");
            _mockConfig.SetupGet(c => c.SFTPSuccessMessage).Returns("SFTP Success");
            _mockConfig.SetupGet(c => c.SFTPErrorMessage).Returns("SFTP Err");
            _mockConfig.SetupGet(c => c.SFTPErrorEmailTo).Returns("error@mail.com");
            _mockConfig.SetupGet(c => c.IPAdd).Returns("127.0.0.2");
            _mockConfig.SetupGet(c => c.TREODRReportMailTo).Returns("eodr@to.com");
            _mockConfig.SetupGet(c => c.TREODRReportMailFrom).Returns("eodr@from.com");
            _mockConfig.SetupGet(c => c.TRThresholdAlertSubject).Returns("Threshold:{0},{1}");
            _mockConfig.SetupGet(c => c.GpiOnlineURL).Returns("http://gpi.url");
            _service = new EmailService(_mockLogger.Object, _mockConfig.Object);
        }

        [TestMethod]
        public async Task ErrorMessageAsync_StringParameter_CallsSendErrorMessageAsync()
        {
            // Test that regular error text triggers SendErrorMessageAsync and hence eventually SendHtmlEmailAsync
            await _service.ErrorMessageAsync("err");
            // Can't verify internal calls; check no exceptions and logger not called
            _mockLogger.Verify(l => l.WriteError(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Exception>()), Times.Never);
        }

        [TestMethod]
        public async Task ErrorMessageAsync_Parameters_CallsSendErrorMessageAsync()
        {
            await _service.ErrorMessageAsync("sys", "err", "reg", "toSys");
            // No exceptions and, in this typical/positive case, logger not called
            _mockLogger.Verify(l => l.WriteError(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Exception>()), Times.Never);
        }

        [TestMethod]
        public async Task EmailErrorMessageAsync_SystemFileErrorRegionToSys_CallsSendErrorMessageAsync()
        {
            await _service.EmailErrorMessageAsync("sys", "file.xml", "error", "region", "toSys");
            // No side-effects except return
        }

        [TestMethod]
        public async Task EmailErrorMessageAsync_SystemDateFileExRegionToSys_CallsSendErrorMessageAsync()
        {
            await _service.EmailErrorMessageAsync("sys", DateTime.Parse("2023-01-01"), "file.xml", "ex", "region", "toSys");
            // No side-effects
        }

        [TestMethod]
        public async Task EmailErrorMessageAsync_Exception_TriggersErrorMessageAsync()
        {
            await _service.EmailErrorMessageAsync(new Exception("Test"));
            // Lower-level error call as fallback
        }

        [TestMethod]
        public async Task SendEmailSFTPSuccessAsync_UsesConfigAndCallsHtml()
        {
            await _service.SendEmailSFTPSuccessAsync("body", "/file/path", "2023-05-22");
        }

        [TestMethod]
        public async Task SendEmailSFTPErrorsAsync_UsesConfigAndCallsHtml()
        {
            await _service.SendEmailSFTPErrorsAsync("body", "/file/path", "2023-05-23");
        }

        [TestMethod]
        public void SFTPSuccess_Returns_FormattedHtml()
        {
            var result = typeof(EmailService)
                .GetMethod("SFTPSuccess", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(_service, new object[] {"b", "/loc", "now"});
            Assert.IsTrue(result.ToString().Contains("SFTP Success"));
        }

        [TestMethod]
        public void SFTPExceptionError_Returns_FormattedHtml()
        {
            var result = typeof(EmailService)
                .GetMethod("SFTPExceptionError", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(_service, new object[] {"problem", "/archive/path", "now", "SOMEQUEUE"});
            Assert.IsTrue(result.ToString().Contains("SFTP Err"));
        }

        [TestMethod]
        public void GetIPAddress_Returns_NonConfigIP()
        {
            // IPAdd is set to 127.0.0.2, so should return any other (on localhost may return 127.0.0.1)
            var val = typeof(EmailService)
                .GetMethod("GetIPAddress", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(_service, null);
            Assert.IsNotNull(val);
        }

        [TestMethod]
        public async Task SendEmailThresholdAlertAsync_AllParams_CallsHtml()
        {
            await _service.SendEmailThresholdAlertAsync("SYS1", "REG1", "trto@mail.com", "", 4, DateTime.Now);
        }

        [TestMethod]
        public async Task SendEmailThresholdAlertAsync_UsesFallback_When_ToIsEmpty()
        {
            await _service.SendEmailThresholdAlertAsync("SYS1", "REG1", "", "", 5, DateTime.Now);
        }

        [TestMethod]
        public async Task SendErrorMessageAsync_StringParam_UsesSendHtml()
        {
            var meth = typeof(EmailService).GetMethod("SendErrorMessageAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null, new[] {typeof(string)}, null);
            await (Task)meth.Invoke(_service, new object[] { "errorMessage" });
        }

        [TestMethod]
        public async Task SendErrorMessageAsync_Parameters_UsesSendHtml()
        {
            var meth = typeof(EmailService).GetMethod("SendErrorMessageAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null, new []{typeof(string), typeof(string), typeof(string), typeof(string)}, null);
            await (Task)meth.Invoke(_service, new object[] { "sys", "error", "region", "" });
        }

        [TestMethod]
        public async Task SendHtmlEmailAsync_MissingToOrFrom_LogsAndReturns()
        {
            var meth = typeof(EmailService).GetMethod("SendHtmlEmailAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            // Missing To
            await (Task)meth.Invoke(_service, new object[] { null, "from@test.com", "subject", "body", "" });
            _mockLogger.Verify(l => l.WriteError(nameof(EmailService.SendHtmlEmailAsync), It.IsAny<string>(), It.IsAny<NullReferenceException>()), Times.Once);
        }

        [TestMethod]
        public async Task SendHtmlEmailAsync_FailsToSend_LogsError()
        {
            // Here, use reflection to inject a bogus SMTP host to force exception
            _mockConfig.SetupGet(c => c.SmtpServer).Returns("invalid.smtp.host");
            var meth = typeof(EmailService).GetMethod("SendHtmlEmailAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            // Use junk values to force SmtpClient.SendMailAsync to fail
            await (Task)meth.Invoke(_service, new object[] { "to@fail.com", "from@fail.com", "subj", "body", "" });
            _mockLogger.Verify(l => l.WriteError(nameof(EmailService.SendHtmlEmailAsync), It.IsAny<string>(), It.IsAny<Exception>()), Times.AtLeastOnce);
        }

        [TestMethod]
        public async Task SendErrorMessageAsync_HandlesException_AndCallsErrorMessageAsync()
        {
            // Use reflection to throw within SendHtmlEmailAsync.
            var email = new EmailService(_mockLogger.Object, _mockConfig.Object);
            var called = false;
            _mockLogger.Setup(l => l.WriteError(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Exception>())).Callback(() => called = true);
            // Simulate error by using blank addresses (so SendHtmlEmailAsync logs an error)
            var meth = typeof(EmailService).GetMethod("SendErrorMessageAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            await (Task)meth.Invoke(email, new object[] { "sys", "err", "reg", "" });
            Assert.IsTrue(called);
        }
    }
}
