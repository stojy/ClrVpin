using System.Linq;
using ClrVpin.Feeder;
using ClrVpin.Models.Feeder.Vps;
using NUnit.Framework;

namespace ClrVpin.Tests.Feeder;

[TestFixture]
internal class FeederUtilsTests
{
    [Test]
    [TestCase(new [] {"first"}, "first")]
    [TestCase(new [] {"short", "longer"}, "short")]
    [TestCase(new [] {"longer", "short"}, "short")]
    [TestCase(new [] {"autho1", "JP's autho2"}, "autho1")]
    [TestCase(new [] {"JP's autho1", "autho2"}, "autho2")]
    [TestCase(new [] {"Cactus Canyon Continued (Bally 1998)", "Cactus Canyon (Bally 1998)"}, "Cactus Canyon (Bally 1998)")]
    public void TestGetUniqueName(string[] names, string expectedName)
    {
        var onlineGames = names.Select(name => new OnlineGame { Name = name }).ToList();
            
        var uniqueName = FeederUtils.GetUniqueGame(onlineGames);

        Assert.That(uniqueName.Name, Is.EqualTo(expectedName));
    }
}