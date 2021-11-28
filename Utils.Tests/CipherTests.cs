using System;
using NUnit.Framework;

namespace Utils.Tests
{
    [TestFixture]
    public class CipherTests
    {
        [Test]
        public void TestEncrypt()
        {
            var encrypted = Cipher.Encrypt("hello", "password");
            var decrypted = Cipher.Decrypt(encrypted, "password");

            var encrypted2 = Cipher.Encrypt("hello", "password");
            var decrypted2 = Cipher.Decrypt(encrypted, "password");

            Assert.That(encrypted, Is.Not.EqualTo(encrypted2));
            Assert.That(decrypted, Is.EqualTo("hello"));
            Assert.That(decrypted2, Is.EqualTo("hello"));
        }

        [Test]
        public void TestCreateKey()
        {
            var encrypted = Cipher.Encrypt("spreadsheetKey", "password");
            Console.WriteLine(encrypted);

            Assert.That(encrypted, Is.Not.Null);
        }
    }
}
