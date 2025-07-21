using Microsoft.VisualStudio.TestTools.UnitTesting;
using GPI.TransactionRecon.BusinessLogic;
using System.Collections.Generic;

namespace GPI.TransactionRecon.Tests
{
    [TestClass]
    public class SFTPRetryDataTests
    {
        [TestMethod]
        public void SFTPRetryCount_GetSet_ShouldWorkCorrectly()
        {
            var retryData = new SFTPRetryData();
            retryData.SFTPRetryCount = 5;
            Assert.AreEqual(5, retryData.SFTPRetryCount);
        }

        [TestMethod]
        public void SFTPFlag_GetSet_ShouldWorkCorrectly()
        {
            var retryData = new SFTPRetryData();
            retryData.SFTPFlag = true;
            Assert.IsTrue(retryData.SFTPFlag);

            retryData.SFTPFlag = false;
            Assert.IsFalse(retryData.SFTPFlag);
        }

        [TestMethod]
        public void epdCountFlag_GetSet_ShouldWorkCorrectly()
        {
            var retryData = new SFTPRetryData();
            var eventData = new OnEventData
            {
                BusinessUnit = "BU1",
                SourceSystem = "SYS1"
            };

            var dict = new Dictionary<OnEventData, int>(new OnEventDataComparer())
            {
                { eventData, 3 }
            };

            retryData.epdCountFlag = dict;

            Assert.IsNotNull(retryData.epdCountFlag);
            Assert.AreEqual(3, retryData.epdCountFlag[eventData]);
        }

        [TestMethod]
        public void OnEventData_GetSet_ShouldWorkCorrectly()
        {
            var eventData = new OnEventData
            {
                BusinessUnit = "BU123",
                SourceSystem = "SYS456"
            };

            Assert.AreEqual("BU123", eventData.BusinessUnit);
            Assert.AreEqual("SYS456", eventData.SourceSystem);
        }
    }

    // Optional: Comparer for Dictionary key equality
    public class OnEventDataComparer : IEqualityComparer<OnEventData>
    {
        public bool Equals(OnEventData x, OnEventData y)
        {
            return x.BusinessUnit == y.BusinessUnit && x.SourceSystem == y.SourceSystem;
        }

        public int GetHashCode(OnEventData obj)
        {
            return (obj.BusinessUnit + obj.SourceSystem).GetHashCode();
        }
    }
}
