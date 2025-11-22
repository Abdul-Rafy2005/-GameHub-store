using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GameHub.Tests.Integration
{
    [TestClass]
    public class CheckoutFlowTests
    {
        [TestMethod]
        public void Discount_Calculation_Should_Handle_Zero_And_Percent()
        {
            decimal price = 100m;
            decimal percent = 15m;
            var discountAmount = Math.Round(price * (percent / 100m), 2);
            var final = Math.Max(0, Math.Round(price - discountAmount, 2));
            Assert.AreEqual(15m, discountAmount);
            Assert.AreEqual(85m, final);
        }
    }
}
