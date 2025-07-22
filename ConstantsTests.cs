using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Configuration;
using System.Reflection;
using System;
using GPI.TransactionRecon.Logger;

namespace GPI.TransactionRecon.Logger.Tests
{
    [TestClass]
    public class AppConfigurationTests
    {
        private NameValueCollection _originalAppSettings;
        private ConnectionStringSettingsCollection _originalConnectionStrings;

        [TestInitialize]
        public void SetUp()
        {
            // Store original
            _originalAppSettings = GetAppSettings();
            _originalConnectionStrings = GetConnectionStrings();

            // Inject test AppSettings & ConnectionStrings
            var testAppSettings = new NameValueCollection
            {
                ["XMLFileFormat"] = "*.xml",
                ["CSVFileFormat"] = "*.csv",
                ["TXTFileFormat"] = "*.txt",
                ["EXCEL"] = "yes",
                ["SmtpServer"] = "mailhost",
                ["TRMailFrom"] = "from@mail.com",
                ["TRMailTo"] = "to@mail.com",
                ["TRMailSubject"] = "subject",
                ["ScheduledTime"] = "23:00",
                ["TRReportLocation"] = "C:\\report",
                ["TRLogsPath"] = "C:\\logs",
                ["TRResourcePath"] = "C:\\res",
                ["TRReport"] = "REPORT",
                ["TREODRReportMailTo"] = "eodr@to.com",
                ["TREODRReportMailFrom"] = "eodr@from.com",
                ["TREODRReportcountryManagers"] = "mgrs",
                ["TREODRReportSubject"] = "eodrSubject",
                ["TREODRReportBody"] = "eodrBody",
                ["IsTREnabled"] = "Y",
                ["TRIgnoreStatusList"] = "IGNORED",
                ["BuList_ABA"] = "aba",
                ["BuList_BLINK"] = "blink",
                ["SysList_TRAXNA"] = "traxna",
                ["BuList_TRAXNA"] = "butraxna",
                ["ParallelProcessFlag"] = "true",
                ["SFTPRetryCount"] = "3",
                ["SFTPRetryDelay"] = "5",
                ["SFTPSuccessMessage"] = "OK",
                ["SFTPErrorMessage"] = "ERR",
                ["SFTPErrorEmailTo"] = "sftp@err.com",
                ["IPAdd"] = "127.0.0.1",
                ["TRThresholdAlertSubject"] = "alert",
                ["GpiOnlineURL"] = "gpi.com",
                ["AcceptableFileFormats"] = ".xml",
                ["BU_ID"] = "BUID",
                ["GPI_Systems"] = "GPISYS",
                ["Ignore_status_BU"] = "I_BU",
                ["IgnoreStatusList_Approved"] = "appr",
                ["IgnoreStatusList_Unapproved"] = "unappr",
                ["EPD_system"] = "epd",
                ["SFTP_Systems"] = "sftpsys",
                ["SysList_TRAXLATAM"] = "latam",
                ["BuList_TRAXLATAM"] = "blatam",
                ["BuList_EBAO"] = "ebao",
                ["BuList_BPM"] = "bpm",
                ["BuList_EBAODC"] = "ebaodc",
                ["BuList_COUPA"] = "coupa",
                ["BuList_POLISYASIA"] = "polisasia",
                ["BuList_ENARASPAC"] = "enaraspac",
                ["BuList_CCA"] = "cca",
                ["BuList_ICONFIANZA"] = "iconfianza",
                ["SysList_TRAXASPAC"] = "sysaspac",
                ["BuList_TRAXASPAC"] = "buaspac",
                ["SysList_TRAXEMEA"] = "sysemea",
                ["BuList_TRAXEMEA"] = "buemea",
                ["IntEnt_TRAXEMEA"] = "intemea",
                ["GLTOAPBU_SYS"] = "gltoap",
                // For negative test
                ["NonExistingKey"] = null
            };
            SetAppSettings(testAppSettings);

            var conns = new ConnectionStringSettingsCollection
            {
                new ConnectionStringSettings("QADB", "test-connection-string")
            };
            SetConnectionStrings(conns);

            // Mock secureAppSettings section using reflection
            var secureSection = new NameValueCollection
            {
                ["SFTPUser_MOCKSYS"] = "sftpUser",
                ["SFTPPassword_MOCKSYS"] = "sftpPwd"
            };
            SetConfigSection("secureAppSettings", secureSection);
        }

        [TestCleanup]
        public void TearDown()
        {
            SetAppSettings(_originalAppSettings);
            SetConnectionStrings(_originalConnectionStrings);
            SetConfigSection("secureAppSettings", null);
        }

        [TestMethod]
        public void Properties_Return_All_Expected_Values()
        {
            var config = new AppConfiguration();
            Assert.AreEqual("*.xml", config.XMLFileFormat);
            Assert.AreEqual("*.csv", config.CSVFileFormat);
            Assert.AreEqual("*.txt", config.TXTFileFormat);
            Assert.AreEqual("yes", config.EXCEL);
            Assert.AreEqual("mailhost", config.SmtpServer);
            Assert.AreEqual("from@mail.com", config.TRMailFrom);
            Assert.AreEqual("to@mail.com", config.TRMailTo);
            Assert.AreEqual("subject", config.TRMailSubject);
            Assert.AreEqual("23:00", config.ScheduledTime);
            Assert.AreEqual("C:\\report", config.TRReportLocation);
            Assert.AreEqual("C:\\logs", config.TRLogsPath);
            Assert.AreEqual("C:\\res", config.TRResourcePath);
            Assert.AreEqual("REPORT", config.TRReport);
            Assert.AreEqual("eodr@to.com", config.TREODRReportMailTo);
            Assert.AreEqual("eodr@from.com", config.TREODRReportMailFrom);
            Assert.AreEqual("mgrs", config.TREODRReportcountryManagers);
            Assert.AreEqual("eodrSubject", config.TREODRReportSubject);
            Assert.AreEqual("eodrBody", config.TREODRReportBody);
            Assert.AreEqual("Y", config.IsTREnabled);
            Assert.AreEqual("IGNORED", config.TRIgnoreStatusList);
            Assert.AreEqual("aba", config.BuList_ABA);
            Assert.AreEqual("blink", config.BuList_BLINK);
            Assert.AreEqual("traxna", config.SysList_TRAXNA);
            Assert.AreEqual("butraxna", config.BuList_TRAXNA);
            Assert.AreEqual("true", config.ParallelProcessFlag);
            Assert.AreEqual("3", config.SFTPRetryCount);
            Assert.AreEqual("5", config.SFTPRetryDelay);
            Assert.AreEqual("OK", config.SFTPSuccessMessage);
            Assert.AreEqual("ERR", config.SFTPErrorMessage);
            Assert.AreEqual("sftp@err.com", config.SFTPErrorEmailTo);
            Assert.AreEqual("127.0.0.1", config.IPAdd);
            Assert.AreEqual("alert", config.TRThresholdAlertSubject);
            Assert.AreEqual("gpi.com", config.GpiOnlineURL);
            Assert.AreEqual(".xml", config.AcceptableFileFormats);
            Assert.AreEqual("BUID", config.BU_ID);
            Assert.AreEqual("GPISYS", config.GPI_Systems);
            Assert.AreEqual("I_BU", config.Ignore_status_BU);
            Assert.AreEqual("appr", config.IgnoreStatusList_Approved);
            Assert.AreEqual("unappr", config.IgnoreStatusList_Unapproved);
            Assert.AreEqual("test-connection-string", config.DatabaseConnectionString);
            Assert.AreEqual("epd", config.EPD_system);
            Assert.AreEqual("sftpsys", config.SFTP_Systems);
            Assert.AreEqual("latam", config.SysList_TRAXLATAM);
            Assert.AreEqual("blatam", config.BuList_TRAXLATAM);
            Assert.AreEqual("ebao", config.BuList_EBAO);
            Assert.AreEqual("bpm", config.BuList_BPM);
            Assert.AreEqual("ebaodc", config.BuList_EBAODC);
            Assert.AreEqual("coupa", config.BuList_COUPA);
            Assert.AreEqual("polisasia", config.BuList_POLISYASIA);
            Assert.AreEqual("enaraspac", config.BuList_ENARASPAC);
            Assert.AreEqual("cca", config.BuList_CCA);
            Assert.AreEqual("iconfianza", config.BuList_ICONFIANZA);
            Assert.AreEqual("sysaspac", config.SysList_TRAXASPAC);
            Assert.AreEqual("buaspac", config.BuList_TRAXASPAC);
            Assert.AreEqual("sysemea", config.SysList_TRAXEMEA);
            Assert.AreEqual("buemea", config.BuList_TRAXEMEA);
            Assert.AreEqual("intemea", config.IntEnt_TRAXEMEA);
            Assert.AreEqual("gltoap", config.GLTOAPBU_SYS);
        }

        [TestMethod]
        public void GetAppSetting_Returns_Value_And_Null()
        {
            var config = new AppConfiguration();
            Assert.AreEqual("*.txt", config.GetAppSetting("TXTFileFormat"));
            Assert.IsNull(config.GetAppSetting("NonExistingKey"));
        }

        [TestMethod]
        public void GetSftpUser_Returns_User_And_Empty_When_NotFound()
        {
            var config = new AppConfiguration();
            // Positive: key present in secureAppSettings
            Assert.AreEqual("sftpUser", config.GetSftpUser("MOCKSYS"));
            // Negative: key absent, returns empty
            Assert.AreEqual(string.Empty, config.GetSftpUser("NOT_FOUND"));
        }

        [TestMethod]
        public void GetSftpPassword_Returns_Password_And_Empty_When_NotFound()
        {
            var config = new AppConfiguration();
            Assert.AreEqual("sftpPwd", config.GetSftpPassword("MOCKSYS"));
            Assert.AreEqual(string.Empty, config.GetSftpPassword("NOPE"));
        }

        #region Reflection/Private Helpers (Test Isolation)

        // Because ConfigurationManager.AppSettings is static and readonly, hack via reflection.
        // NOTE: Some runners/platforms may restrict this hack; in those cases use Microsoft Fakes or wrap in interface.
        private static NameValueCollection GetAppSettings()
        {
            var field = typeof(ConfigurationManager).GetField("s_appSettings", BindingFlags.NonPublic | BindingFlags.Static)
                ?? typeof(ConfigurationManager).GetField("AppSettings", BindingFlags.NonPublic | BindingFlags.Static);
            return (NameValueCollection)field?.GetValue(null);
        }

        private static void SetAppSettings(NameValueCollection nvc)
        {
            var field = typeof(ConfigurationManager).GetField("s_appSettings", BindingFlags.NonPublic | BindingFlags.Static)
                ?? typeof(ConfigurationManager).GetField("AppSettings", BindingFlags.NonPublic | BindingFlags.Static);
            field?.SetValue(null, nvc);
        }

        private static ConnectionStringSettingsCollection GetConnectionStrings()
        {
            var settings = typeof(ConfigurationManager).GetProperty("ConnectionStrings", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                ?.GetValue(null) as ConnectionStringSettingsCollection;
            return settings;
        }

        private static void SetConnectionStrings(ConnectionStringSettingsCollection conns)
        {
            // .NET Framework only: hack with reflection if possible
            var settingsField = typeof(ConfigurationManager).GetField("s_connectionStrings", BindingFlags.Static | BindingFlags.NonPublic)
                ?? typeof(ConfigurationManager).GetField("ConnectionStrings", BindingFlags.Static | BindingFlags.NonPublic);
            settingsField?.SetValue(null, conns);
        }

        private static void SetConfigSection(string sectionName, object value)
        {
            // Update Section via System.Configuration.ConfigurationManager
            var configSystemField = typeof(ConfigurationManager).GetField("s_configSystem", BindingFlags.NonPublic | BindingFlags.Static);
            var configSystem = configSystemField?.GetValue(null);
            if (configSystem != null)
            {
                var sectionsField = configSystem.GetType().GetField("sections", BindingFlags.Instance | BindingFlags.NonPublic);
                var sections = sectionsField?.GetValue(configSystem) as IDictionary<string, object>;
                if (sections != null)
                {
                    if (value == null && sections.ContainsKey(sectionName)) sections.Remove(sectionName);
                    else sections[sectionName] = value;
                }
            }
        }

        #endregion
    }
}
