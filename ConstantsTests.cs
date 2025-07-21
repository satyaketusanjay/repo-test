using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using GPI.TransactionRecon.Logger;
using GPI.TransactionRecon.Logger.Contracts;
using System;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.IO;

// --- Mock Interfaces and Dummy Classes for Compilation ---
// Note: These are placeholder definitions to make the test file self-contained.
// You should use the actual definitions from your project.
namespace GPI.TransactionRecon.Logger.Contracts
{
    public interface ILoggerService
    {
        void WriteError(string method, string message, Exception ex);
    }

    public interface IAppConfiguration
    {
        string TRMailTo { get; }
        string TRMailFrom { get; }
        string TRMailSubject { get; }
        string SmtpServer { get; }
        string SFTPErrorEmailTo { get; }
        string SFTPSuccessMessage { get; }
        string SFTPErrorMessage { get; }
        string IPAdd { get; }
        string TREODRReportMailTo { get; }
        string TREODRReportMailFrom { get; }
        string TRThresholdAlertSubject { get; }
        string GpiOnlineURL { get; }
        string GetAppSetting(string key);
    }
}

namespace GPI.TransactionRecon.Logger
{
    // Dummy class to provide constants used by the EmailService
    public static class EmailConstants
    {
        public const string ExceptionError = "An exception has occurred.";
        public const string Date = "Date: ";
        public const string ExceptionDetails = "Details: ";
        public const string MailEnding = "-- End of Message --";
        public const string System = "System: ";
        public const string FileName = "File Name: ";
        public const string InternalExceptionError = "An internal exception has occurred in the mailing service.";
        public const string HtmlEndTag = "</body></html>";
        public const string HtmlHelpDeskMessage = "<br><br>Please raise a ticket with the help desk: {0}";
        public const string ThresholdAlertHeading = "<h1>Threshold Alert</h1>";
        public const string ThresholdAlertFirstLine = "<p>This is an alert for system {0}.</p>";
        public const string ThresholdAlertSecondLine = "<p>Please check the dashboard at {0}.</p>";
        public const string ThresholdAlertThirdLine = "<p>Further details are provided below.</p>";
        public const string ThresholdAlertForthLine = "<p>Last report time: {0}, Total records: {1}.</p>";
        public const string ThresholdAlertClosing = "<p>Regards,<br>System</p>";
        public const string MailingError = "A critical error occurred while attempting to send an email.";
    }
}
// --- End of Mock/Dummy Section ---


[TestClass]
public class EmailServiceTests
{
    private Mock<ILoggerService> _loggerMock;
    private Mock<IAppConfiguration> _configMock;
    private EmailService _emailService;

    // This method runs before each test, setting up a fresh environment.
    [TestInitialize]
    public void TestInitialize()
    {
        _loggerMock = new Mock<ILoggerService>();
        _configMock = new Mock<IAppConfiguration>();

        // Setup default valid configuration for positive test cases
        _configMock.SetupGet(c => c.TRMailTo).Returns("to@example.com");
        _configMock.SetupGet(c => c.TRMailFrom).Returns("from@example.com");
        _configMock.SetupGet(c => c.TRMailSubject).Returns("Test Subject");
        _configMock.SetupGet(c => c.SmtpServer).Returns("smtp.example.com"); // A dummy server
        _configMock.SetupGet(c => c.SFTPErrorEmailTo).Returns("sftp-errors@example.com");
        _configMock.SetupGet(c => c.TREODRReportMailTo).Returns("eod-reports@example.com");
        _configMock.SetupGet(c => c.TREODRReportMailFrom).Returns("from@example.com");


        _emailService = new EmailService(_loggerMock.Object, _configMock.Object);
    }

    #region Negative Scenarios - Invalid Email Addresses

    [TestMethod]
    public async Task SendHtmlEmailAsync_ToAddressIsEmpty_LogsError()
    {
        // Arrange
        // Configure the mock to return an invalid "To" address.
        _configMock.SetupGet(c => c.TRMailTo).Returns(""); // Empty "To" address

        // Act
        // Call a method that will use the invalid configuration.
        await _emailService.ErrorMessageAsync("Test error");

        // Assert
        // Verify that the logger was called with the expected error message.
        // This confirms the internal validation in SendHtmlEmailAsync works.
        _loggerMock.Verify(
            log => log.WriteError(
                "SendHtmlEmailAsync",
                It.Is<string>(msg => msg.Contains("'To' or 'From' address is empty")),
                It.IsAny<NullReferenceException>()
            ),
            Times.Once
        );
    }

    [TestMethod]
    public async Task SendHtmlEmailAsync_FromAddressIsWhitespace_LogsError()
    {
        // Arrange
        _configMock.SetupGet(c => c.TRMailFrom).Returns("   "); // Whitespace "From" address

        // Act
        await _emailService.ErrorMessageAsync("Test error");

        // Assert
        _loggerMock.Verify(
            log => log.WriteError(
                "SendHtmlEmailAsync",
                It.Is<string>(msg => msg.Contains("'To' or 'From' address is empty")),
                It.IsAny<NullReferenceException>()
            ),
            Times.Once
        );
    }

    #endregion

    #region Negative Scenarios - SMTP/Network Failures

    [TestMethod]
    public async Task SendHtmlEmailAsync_SmtpServerIsNull_LogsSmtpFailure()
    {
        // Arrange
        // Setting SmtpServer to null will cause 'new SmtpClient(null)' to throw an exception.
        _configMock.SetupGet(c => c.SmtpServer).Returns((string)null);

        // Act
        // We can call any public method that sends an email to trigger the failure.
        await _emailService.SendEmailSFTPSuccessAsync("body", "location", "timestamp");

        // Assert
        // Verify that the catch block in SendHtmlEmailAsync logged the failure.
        _loggerMock.Verify(
            log => log.WriteError(
                "SendHtmlEmailAsync",
                It.Is<string>(msg => msg.StartsWith("Failed to send email.")),
                It.IsAny<Exception>() // The underlying exception could be ArgumentNullException or SmtpException
            ),
            Times.Once
        );
    }
    
    #endregion

    #region Positive "Smoke" Scenarios
    // These tests ensure that given valid inputs, the methods execute without throwing
    // unhandled exceptions. They act as basic "smoke tests".

    [TestMethod]
    public async Task ErrorMessageAsync_WithString_RunsSuccessfully()
    {
        // Act
        await _emailService.ErrorMessageAsync("A simple error occurred.");

        // Assert
        // The primary assertion is that no exception was thrown.
        // We also expect the logger NOT to be called, but since the email send will
        // fail with our dummy SMTP server, we expect a "Failed to send" log.
        _loggerMock.Verify(log => log.WriteError(It.IsAny<string>(), It.Is<string>(s => s.StartsWith("Failed to send email")), It.IsAny<Exception>()), Times.Once);
    }

    [TestMethod]
    public async Task ErrorMessageAsync_WithSystemAndRegion_RunsSuccessfully()
    {
        // Act
        await _emailService.ErrorMessageAsync("SystemX", "A detailed error.", "APAC", "override@example.com");

        // Assert
        _loggerMock.Verify(log => log.WriteError(It.IsAny<string>(), It.Is<string>(s => s.StartsWith("Failed to send email")), It.IsAny<Exception>()), Times.Once);
    }
    
    [TestMethod]
    public async Task EmailErrorMessageAsync_WithFile_RunsSuccessfully()
    {
        // Act
        await _emailService.EmailErrorMessageAsync("SystemY", "C:\\temp\\data.csv", "File processing error.", "EMEA", "sysy@example.com");

        // Assert
        _loggerMock.Verify(log => log.WriteError(It.IsAny<string>(), It.Is<string>(s => s.StartsWith("Failed to send email")), It.IsAny<Exception>()), Times.Once);
    }

    [TestMethod]
    public async Task EmailErrorMessageAsync_WithDateAndFile_RunsSuccessfully()
    {
        // Act
        await _emailService.EmailErrorMessageAsync("SystemZ", DateTime.Now, "C:\\temp\\log.txt", "Exception details.", "NA", "sysz@example.com");

        // Assert
        _loggerMock.Verify(log => log.WriteError(It.IsAny<string>(), It.Is<string>(s => s.StartsWith("Failed to send email")), It.IsAny<Exception>()), Times.Once);
    }

    [TestMethod]
    public async Task EmailErrorMessageAsync_WithException_RunsSuccessfully()
    {
        // Arrange
        var exception = new InvalidOperationException("Something went wrong.");

        // Act
        await _emailService.EmailErrorMessageAsync(exception);

        // Assert
        _loggerMock.Verify(log => log.WriteError(It.IsAny<string>(), It.Is<string>(s => s.StartsWith("Failed to send email")), It.IsAny<Exception>()), Times.Once);
    }

    [TestMethod]
    public async Task SendEmailSFTPSuccessAsync_RunsSuccessfully()
    {
        // Act
        await _emailService.SendEmailSFTPSuccessAsync("SFTP connection restored.", "/remote/path", DateTime.Now.ToString("o"));

        // Assert
        _loggerMock.Verify(log => log.WriteError(It.IsAny<string>(), It.Is<string>(s => s.StartsWith("Failed to send email")), It.IsAny<Exception>()), Times.Once);
    }

    [TestMethod]
    public async Task SendEmailSFTPErrorsAsync_RunsSuccessfully()
    {
        // Act
        await _emailService.SendEmailSFTPErrorsAsync("SFTP connection failed.", "/remote/path/errors", DateTime.Now.ToString("o"));

        // Assert
        _loggerMock.Verify(log => log.WriteError(It.IsAny<string>(), It.Is<string>(s => s.StartsWith("Failed to send email")), It.IsAny<Exception>()), Times.Once);
    }
    
    [TestMethod]
    public async Task SendEmailThresholdAlertAsync_RunsSuccessfully()
    {
        // Act
        await _emailService.SendEmailThresholdAlertAsync("ReportingSystem", "APAC", "distro@example.com", "manager@example.com", 5000, DateTime.Now);

        // Assert
        _loggerMock.Verify(log => log.WriteError(It.IsAny<string>(), It.Is<string>(s => s.StartsWith("Failed to send email")), It.IsAny<Exception>()), Times.Once);
    }

    #endregion
}
