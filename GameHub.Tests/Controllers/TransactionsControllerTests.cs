using System;
using System.Web.Mvc;
using GameHub.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GameHub.Tests.Controllers
{
    [TestClass]
    public class TransactionsControllerTests
    {
        [TestMethod]
        public void Purchase_Should_Return_BadRequest_For_Invalid_Input()
        {
            var controller = new TransactionsController();
            var result = controller.Purchase(0, 0, null, null) as HttpStatusCodeResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(400, result.StatusCode);
        }

        [TestMethod]
        public void Checkout_Should_Return_View_With_Model()
        {
            var controller = new TransactionsController();
            var result = controller.Checkout(0, null, null) as HttpStatusCodeResult;
            Assert.IsNotNull(result);
        }
    }
}
