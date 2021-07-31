using System;
using System.Xml.Linq;
using System.Xml.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UtilsTests
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
            var xdoc = sheep.SerializeToXDoc();
            var ser = "<Animal " +
                      "xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                      "xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  " +
                      "<Name>Sheep</Name>\r\n  <Legs>4</Legs>\r\n  " +
                      "<Nutrition>Herbivore</Nutrition>\r\n</Animal>";

            Assert.AreEqual(xdoc.ToString(), ser);
            Assert.IsInstanceOfType(xdoc, typeof(XDocument));
        }

        [TestMethod]
        public void Tests_DeserializeFromXDoc()
        {
            var Sheep = new Animal
            {
                Name = "Sheep",
                Legs = 4,
                Nutrition = Nutrition.Herbivore,
                Names = new [] { "a", "b"}
            };
            var serializeToXDoc = Sheep.SerializeToXDoc();
            var des = serializeToXDoc.DeserializeFromXDoc<Animal>();

            Assert.AreEqual(des.Name, Sheep.Name);
            Assert.AreEqual(des.Nutrition, Sheep.Nutrition);
            Assert.AreEqual(des.Legs, Sheep.Legs);
            Assert.AreNotSame(des, Sheep);
        }

        [TestMethod]
        public void Test1()
        {
            var a = new Animal {Name = "a"};
            var b = a.Name;

            b = "b";

            Assert.AreNotEqual(a.Name, b);

        }
    }

    public static class ExtensionMethods
    {
        public static T DeserializeFromXDoc<T>(this XDocument source)
        {
            if (source == null || source.Root == null)
                return default(T);

            using (var reader = source.Root.CreateReader())
                return (T)new XmlSerializer(typeof(T)).Deserialize(reader);
        }

        public static XDocument SerializeToXDoc<T>(this T source)
        {
            if (source == null)
                return null;

            var doc = new XDocument();
            using (var writer = doc.CreateWriter())
                new XmlSerializer(typeof(T)).Serialize(writer, source);

            return doc;
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