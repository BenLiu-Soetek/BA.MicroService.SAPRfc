using BA.MicroService.SAPRfcTests.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SapRfcMicroservice;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SapRfcMicroservice.Tests
{
    [TestClass]
    public class RfcResultTests
    {
        [TestMethod]
        public void SampleRfcResult_ShouldHaveExpectedValues()
        {
            var result = SampleData.SampleRfcResult();

            Assert.IsTrue(result.Success);
            Assert.AreEqual("Operation successful", result.Exports["MESSAGE"]);
            Assert.AreEqual(5, result.Exports["COUNT"]);

            Assert.IsTrue(result.Tables.ContainsKey("ITEMS"));
            var items = result.Tables["ITEMS"];
            Assert.AreEqual(2, items.Count);

            Assert.AreEqual(1001, items[0]["ID"]);
            Assert.AreEqual("Item A", items[0]["DESCRIPTION"]);
            Assert.AreEqual(10, items[0]["QUANTITY"]);

            Assert.AreEqual(1002, items[1]["ID"]);
            Assert.AreEqual("Item B", items[1]["DESCRIPTION"]);
            Assert.AreEqual(5, items[1]["QUANTITY"]);
        }
    }
}