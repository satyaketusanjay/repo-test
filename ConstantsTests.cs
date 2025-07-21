using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using GPI.TransactionRecon.Logger;
using GPI.TransactionRecon.Logger.Contracts;
using System;
using System.Net.Mail;
using System.Threading.Tasks;
using System.IO;

// --- Mock Interfaces and Dummy Classes for Compilation ---
// Note: These are placeholder definitions to make the test file self-contained.
// You should use the actual definitions from your project.
namespace GPI.TransactionRecon.Logger.Contracts
{
    public interface ILoggerService
    {
        // Updated to include the fourth parameter as requested.
        void WriteError(string method, string message, Exception ex, string details = "");
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

        // Setup default valid configuration for most test cases
        _configMock.SetupGet(c => c.TRMailTo).Returns("default-to@example.com");
        _configMock.SetupGet(c => c.TRMailFrom).Returns("default-from@example.com");
        _configMock.SetupGet(c => c.TRMailSubject).Returns("Default Subject");
        _configMock.SetupGet(c => c.SmtpServer).Returns("smtp.test-server.com"); // A dummy server that will cause a failure
        _configMock.SetupGet(c => c.SFTPErrorEmailTo).Returns("sftp-errors@example.com");
        _configMock.SetupGet(c => c.TREODRReportMailTo).Returns("eod-reports@example.com");
        _configMock.SetupGet(c => c.TREODRReportMailFrom).Returns("reports-from@example.com");
        _configMock.SetupGet(c => c.TRThresholdAlertSubject).Returns("Threshold Alert: {0} for {1}");
        
        _emailService = new EmailService(_loggerMock.Object, _configMock.Object);
    }

    #region Negative Scenarios - Invalid Configuration

    [TestMethod]
    public async Task SendHtmlEmailAsync_ToAddressIsEmpty_LogsErrorAndReturns()
    {
        // Arrange
        _configMock.SetupGet(c => c.TRMailTo).Returns(""); // Invalid "To" address

        // Act
        // This method internally calls SendHtmlEmailAsync
        await _emailService.ErrorMessageAsync("Test error");

        // Assert
        // Verify the specific error for an empty address is logged.
        _loggerMock.Verify(
            log => log.WriteError(
                "SendHtmlEmailAsync",
                It.Is<string>(msg => msg.Contains("'To' or 'From' address is empty")),
                It.IsAny<NullReferenceException>(),
                ""
            ),
            Times.Once
        );
    }

    [TestMethod]
    public async Task SendHtmlEmailAsync_FromAddressIsWhitespace_LogsErrorAndReturns()
    {
        // Arrange
        _configMock.SetupGet(c => c.TRMailFrom).Returns("   "); // Invalid "From" address

        // Act
        await _emailService.ErrorMessageAsync("Test error");

        // Assert
        _loggerMock.Verify(
            log => log.WriteError(
                "SendHtmlEmailAsync",
                It.Is<string>(msg => msg.Contains("'To' or 'From' address is empty")),
                It.IsAny<NullReferenceException>(),
                ""
            ),
            Times.Once
        );
    }
    
    [TestMethod]
    public async Task SendHtmlEmailAsync_SmtpServerIsNull_LogsSmtpFailure()
    {
        // Arrange
        // Setting SmtpServer to null will cause 'new SmtpClient(null)' to throw an ArgumentNullException.
        _configMock.SetupGet(c => c.SmtpServer).Returns((string)null);

        // Act
        await _emailService.ErrorMessageAsync("Test error");

        // Assert
        // Verify the catch block in SendHtmlEmailAsync logged the failure.
        _loggerMock.Verify(
            log => log.WriteError(
                "SendHtmlEmailAsync",
                It.Is<string>(msg => msg.StartsWith("Failed to send email.")),
                It.IsAny<ArgumentNullException>(),
                ""
            ),
            Times.Once
        );
    }

    #endregion

    #region Positive Scenarios - Email Construction and Logic
    // --- TESTING STRATEGY ---
    // The EmailService class creates its own SmtpClient instance (`new SmtpClient()`),
    // which makes it impossible to mock the email sending process directly with Moq.
    // Therefore, any call to send an email in a test environment will fail.
    //
    // The following tests EMBRACE this failure. They work by:
    // 1. Calling the public methods of EmailService.
    // 2. Letting the internal SmtpClient fail, which is caught by the try-catch block.
    // 3. Verifying that the ILoggerService is called with the CORRECTLY CONSTRUCTED email parameters (To, From, Subject).
    //
    // This approach allows us to test the LOGIC of the EmailService (i.e., that it builds the right email)
    // without needing to change the production code.

    [TestMethod]
    public async Task ErrorMessageAsync_WithSystemAndRegion_ConstructsCorrectSubjectAndRecipient()
    {
        // Arrange
        var system = "SystemX";
        var expectedTo = "override@example.com";
        var expectedSubject = $"{_configMock.Object.TRMailSubject} - {system}";

        // Act
        await _emailService.ErrorMessageAsync(system, "A detailed error.", "APAC", expectedTo);

        // Assert
        _loggerMock.Verify(
            log => log.WriteError(
                "SendHtmlEmailAsync",
                It.Is<string>(msg =>
                    msg.Contains($"To: {expectedTo}") &&
                    msg.Contains($"Subject: {expectedSubject}")
                ),
                It.IsAny<Exception>(),
                ""
            ),
            Times.Once
        );
    }

    [TestMethod]
    public async Task SendEmailSFTPErrorsAsync_UsesFallbackEmail_WhenSFTPErrorEmailToIsNull()
    {
        // Arrange
        var fallbackTo = _configMock.Object.TRMailTo;
        _configMock.SetupGet(c => c.SFTPErrorEmailTo).Returns((string)null); // Simulate null config value

        // Act
        await _emailService.SendEmailSFTPErrorsAsync("SFTP connection failed.", "/remote/path/errors", DateTime.Now.ToString("o"));

        // Assert
        _loggerMock.Verify(
            log => log.WriteError(
                "SendHtmlEmailAsync",
                It.Is<string>(msg => msg.Contains($"To: {fallbackTo}")), // Verify it used the fallback
                It.IsAny<Exception>(),
                ""
            ),
            Times.Once
        );
    }
    
    [TestMethod]
    public async Task SendEmailSFTPErrorsAsync_UsesSpecificSFTPErrorEmail_WhenAvailable()
    {
        // Arrange
        var sftpTo = _configMock.Object.SFTPErrorEmailTo; // "sftp-errors@example.com"

        // Act
        await _emailService.SendEmailSFTPErrorsAsync("SFTP connection failed.", "/remote/path/errors", DateTime.Now.ToString("o"));

        // Assert
        _loggerMock.Verify(
            log => log.WriteError(
                "SendHtmlEmailAsync",
                It.Is<string>(msg => msg.Contains($"To: {sftpTo}")), // Verify it used the specific address
                It.IsAny<Exception>(),
                ""
            ),
            Times.Once
        );
    }

    [TestMethod]
    public async Task SendEmailThresholdAlertAsync_ConstructsCorrectFormattedSubjectAndRecipients()
    {
        // Arrange
        var system = "ReportingSystem";
        var region = "APAC";
        var to = "distro@example.com";
        var cc = "manager@example.com";
        var from = "reports-from@example.com";
        var expectedSubject = "Threshold Alert: ReportingSystem for APAC";

        _configMock.SetupGet(c => c.TREODRReportMailFrom).Returns(from);

        // Act
        await _emailService.SendEmailThresholdAlertAsync(system, region, to, cc, 5000, DateTime.Now);

        // Assert
        _loggerMock.Verify(
            log => log.WriteError(
                "SendHtmlEmailAsync",
                It.Is<string>(msg =>
                    msg.Contains($"To: {to}") &&
                    msg.Contains($"From: {from}") &&
                    msg.Contains($"Subject: {expectedSubject}")
                ),
                It.IsAny<Exception>(),
                ""
            ),
            Times.Once
        );
    }

    [TestMethod]
    public async Task EmailErrorMessageAsync_WithException_ConstructsCorrectEmail()
    {
        // Arrange
        var exception = new InvalidOperationException("Something went wrong.");
        var expectedTo = _configMock.Object.TRMailTo;
        var expectedFrom = _configMock.Object.TRMailFrom;
        var expectedSubject = _configMock.Object.TRMailSubject;

        // Act
        await _emailService.EmailErrorMessageAsync(exception);

        // Assert
        // This method calls another overload, which eventually calls SendHtmlEmailAsync.
        // We verify the final parameters.
        _loggerMock.Verify(
            log => log.WriteError(
                "SendHtmlEmailAsync",
                It.Is<string>(msg =>
                    msg.Contains($"To: {expectedTo}") &&
                    msg.Contains($"From: {expectedFrom}") &&
                    msg.Contains($"Subject: {expectedSubject}")
                ),
                It.IsAny<Exception>(),
                ""
            ),
            Times.Once
        );
    }

    #endregion
}
