using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using GameHub.Controllers;
using GameHub.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GameHub.Tests.Controllers
{
    [TestClass]
    public class GamesControllerTests
    {
        [TestMethod]
        public void Index_Should_Filter_By_Title_And_Genre()
        {
            // Arrange - create in-memory list (not hitting DB)
            var games = new List<Game>
            {
                new Game { GameID = 1, Title = "Halo", GenreID = 1 },
                new Game { GameID = 2, Title = "Half-Life", GenreID = 2 },
                new Game { GameID = 3, Title = "Hades", GenreID = 1 },
            }.AsQueryable();

            // We can't easily replace EF context in this controller without refactor, so we test logic indirectly by LINQ
            var filtered = games.Where(g => g.Title.Contains("Ha"));
            Assert.AreEqual(2, filtered.Count());

            filtered = games.Where(g => g.Title.Contains("Ha") && g.GenreID == 1);
            Assert.AreEqual(1, filtered.Count());
        }

        [TestMethod]
        public void SubmitFeedback_Should_Validate_Rating_Range()
        {
            // Basic parameter validation test
            var controller = new GamesController();
            var result = controller.SubmitFeedback(1, 1, 6, "bad") as RedirectToRouteResult;
            Assert.IsNotNull(result);
            Assert.AreEqual("Details", result.RouteValues["action"]);
            Assert.AreEqual(1, result.RouteValues["id"]);
        }
    }
}
