using System.Linq;
using ClrVpin.Importer;
using ClrVpin.Models.Importer.Vps;
using NUnit.Framework;

namespace ClrVpin.Tests.Importer;

[TestFixture]
internal class ImporterUtilsTests
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
            
        var uniqueName = ImporterUtils.GetUniqueGame(onlineGames);

        Assert.That(uniqueName.Name, Is.EqualTo(expectedName));
    }
}