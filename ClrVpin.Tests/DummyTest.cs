using System;
using NUnit.Framework;
// ReSharper disable All

namespace ClrVpin.Tests
{
    [TestFixture]
    public class DummyTest
    {
        [Test]
        public void InitializationTest()
        {
            // based on https: //www.csharp411.com/c-object-initialization/
            new Derived {Property2 = new Tracker("Base.Instance.Property2")};
        }

        private class Base
        {
            protected Base()
            {
                Console.WriteLine("Base.Instance.Constructor");
                _mField3 = new Tracker("Base.Instance.Field3");
                
                // ReSharper disable once VirtualMemberCallInConstructor
                Virtual();
            }
            static Base()
            {
                Console.WriteLine("Base.Static.Constructor");
            }
            private Tracker _mField1 = new Tracker("Base.Instance.Field1");
            private Tracker _mField2 = new Tracker("Base.Instance.Field2");
            private Tracker _mField3;
            private Tracker Property1 { get; set; } = new Tracker("Base.Instance.Property1");
            public Tracker Property2 { get; set; }
            private static Tracker _sField1 = new Tracker("Base.Static.Field1");
            private static Tracker _sField2 = new Tracker("Base.Static.Field2");
            public virtual void Virtual()
            {
                Console.WriteLine("Base.Instance.Virtual");
            }
        }

        private class Derived : Base
        {
            public Derived()
            {
                Console.WriteLine("Derived.Instance.Constructor");
                _mField3 = new Tracker("Derived.Instance.Field3");
            }
            static Derived()
            {
                Console.WriteLine("Derived.Static.Constructor");
            }
            private Tracker _mField1 = new Tracker("Derived.Instance.Field1");
            private Tracker _mField2 = new Tracker("Derived.Instance.Field2");
            private Tracker _mField3;
            private static Tracker _sField1 = new Tracker("Derived.Static.Field1");
            private static Tracker _sField2 = new Tracker("Derived.Static.Field2");
            public override void Virtual()
            {
                Console.WriteLine("Derived.Instance.Virtual");
            }
        }
        class Tracker
        {
            public Tracker(string text)
            {
                Console.WriteLine(text);
            }
        }
    }
}
