using System;
using System.Xml.Linq;
using System.Xml.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Utils.Tests
{
    [TestClass]
    public class Tests
    {
        [TestMethod]
        public void Tests_SerializeToXDoc()
        {
            var sheep = new Animal
            {
                Name = "Sheep",
                Legs = 4,
                Nutrition = Nutrition.Herbivore,
                Names = new [] { "a", "b"}
            };
            var xDoc = sheep.SerializeToXDoc();
            const string ser = "<Animal xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <Name>Sheep</Name>\r\n  <Legs>4</Legs>\r\n  <Nutrition>Herbivore</Nutrition>\r\n  <names>a</names>\r\n  <names>b</names>\r\n</Animal>";

            Assert.AreEqual(xDoc.ToString(), ser);
            Assert.IsInstanceOfType(xDoc, typeof(XDocument));
        }

        [TestMethod]
        public void Tests_DeserializeFromXDoc()
        {
            var sheep = new Animal
            {
                Name = "Sheep",
                Legs = 4,
                Nutrition = Nutrition.Herbivore,
                Names = new [] { "a", "b"}
            };
            var serializeToXDoc = sheep.SerializeToXDoc();
            var des = serializeToXDoc.DeserializeFromXDoc<Animal>();

            Assert.AreEqual(des.Name, sheep.Name);
            Assert.AreEqual(des.Nutrition, sheep.Nutrition);
            Assert.AreEqual(des.Legs, sheep.Legs);
            Assert.AreNotSame(des, sheep);
        }

        [TestMethod]
        public void Test1()
        {
            var a = new Animal {Name = "a"};
            var b = "b";

            Assert.AreNotEqual(a.Name, b);
        }
    }

    [Serializable]
    public class Animal
    {
        public string Name { get; set; }
        public int Legs { get; set; }
        public Nutrition Nutrition { get; set; }

        [XmlElement("names")]
        public string[] Names { get; set; }
    }

    

    public enum Nutrition
    {
        Herbivore,
        Carnivore,
        Omnivore
    }
}