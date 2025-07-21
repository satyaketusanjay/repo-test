using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using GPI.TransactionRecon.Logger;

namespace GPI.TransactionRecon.Tests
{
    [TestClass]
    public class EmailServiceTests
    {
        private Mock<ILoggerService> _mockLogger;
        private Mock<IAppConfiguration> _mockConfig;
        private EmailService _emailService;

        [TestInitialize]
        public void Setup()
        {
            _mockLogger = new Mock<ILoggerService>();
            _mockConfig = new Mock<IAppConfiguration>();

            _mockConfig.Setup(c => c.TRMailTo).Returns("to@example.com");
            _mockConfig.Setup(c => c.TRMailFrom).Returns("from@example.com");
            _mockConfig.Setup(c => c.TRMailSubject).Returns("Test Subject");
            _mockConfig.Setup(c => c.SmtpServer).Returns("smtp.example.com");
            _mockConfig.Setup(c => c.SFTPSuccessMessage).Returns("SFTP Success");
            _mockConfig.Setup(c => c.SFTPErrorMessage).Returns("SFTP Error");
            _mockConfig.Setup(c => c.IPAdd).Returns("127.0.0.1");

            _emailService = new EmailService(_mockLogger.Object, _mockConfig.Object);
        }

        [TestMethod]
        public async Task ErrorMessageAsync_WithError_ShouldSendEmail()
        {
            await _emailService.ErrorMessageAsync("Test error");

            _mockLogger.Verify(l => l.WriteError(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Exception>()), Times.Never);
        }

        [TestMethod]
        public async Task SendEmailSFTPSuccessAsync_ShouldSendSuccessEmail()
        {
            await _emailService.SendEmailSFTPSuccessAsync("Body", "/path/to/file", "2025-07-21");

            _mockLogger.Verify(l => l.WriteError(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Exception>()), Times.Never);
        }

        [TestMethod]
        public async Task SendEmailSFTPErrorsAsync_ShouldSendErrorEmail()
        {
            await _emailService.SendEmailSFTPErrorsAsync("Body", "/path/to/file", "2025-07-21");

            _mockLogger.Verify(l => l.WriteError(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Exception>()), Times.Never);
        }

        [TestMethod]
        public async Task SendEmailThresholdAlertAsync_WithValidInputs_ShouldSendEmail()
        {
            _mockConfig.Setup(c => c.TREODRReportMailTo).Returns("report@example.com");
            _mockConfig.Setup(c => c.TREODRReportMailFrom).Returns("reportfrom@example.com");
            _mockConfig.Setup(c => c.TRThresholdAlertSubject).Returns("Threshold Alert - {0} - {1}");
            _mockConfig.Setup(c => c.GpiOnlineURL).Returns("http://gpi-online");

            await _emailService.SendEmailThresholdAlertAsync("System", "Region", "", "", 100, DateTime.Now);

            _mockLogger.Verify(l => l.WriteError(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Exception>()), Times.Never);
        }

        [TestMethod]
        public async Task SendHtmlEmailAsync_MissingToOrFrom_ShouldLogError()
        {
            _mockConfig.Setup(c => c.TRMailTo).Returns("");
            _mockConfig.Setup(c => c.TRMailFrom).Returns("");
            _mockConfig.Setup(c => c.TRMailSubject).Returns("Subject");

            await _emailService.ErrorMessageAsync("Test error");

            _mockLogger.Verify(l => l.WriteError(
                nameof(EmailService.SendHtmlEmailAsync),
                It.Is<string>(msg => msg.Contains("Email not sent")),
                It.IsAny<NullReferenceException>()), Times.Once);
        }

        [TestMethod]
        public async Task SendHtmlEmailAsync_ExceptionThrown_ShouldLogError()
        {
            _mockConfig.Setup(c => c.SmtpServer).Returns("invalid.smtp.server");
            _mockConfig.Setup(c => c.TRMailTo).Returns("to@example.com");
            _mockConfig.Setup(c => c.TRMailFrom).Returns("from@example.com");
            _mockConfig.Setup(c => c.TRMailSubject).Returns("Subject");

            await _emailService.ErrorMessageAsync("Test error");

            _mockLogger.Verify(l => l.WriteError(
                nameof(EmailService.SendHtmlEmailAsync),
                It.Is<string>(msg => msg.Contains("Failed to send email")),
                It.IsAny<Exception>()), Times.Once);
        }
    }
}
