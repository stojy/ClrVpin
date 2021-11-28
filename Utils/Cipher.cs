using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Utils
{
    // cipher utilities
    // - encrypted output format:salt + iv + encryptedText.. deliberately includes alt and IV for subsequent decryption
    // - refer https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.rfc2898derivebytes?view=net-6.0
    //         https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.aes?view=net-6.0
    public static class Cipher
    {
        public static string Encrypt(string plainText, string password)
        {
            // generate a derived (hashed) key that's been given the salt and iteration treatment
            var salt = RandomNumberGenerator.GetBytes(KeySize);
            var derivedKey = GetDerivedKey(password, salt);

            // encrypt text using the derived key (and automatically generated initialization vector)
            // - refer https://stackoverflow.com/a/2790721/227110
            using var aes = Aes.Create();
            using var cryptoTransform = aes.CreateEncryptor(derivedKey, aes.IV);
            using var memoryStream = new MemoryStream();
            using var cryptoStream = new CryptoStream(memoryStream, cryptoTransform, CryptoStreamMode.Write);
            using var streamWriter = new StreamWriter(cryptoStream);
            streamWriter.Write(plainText);
            streamWriter.Close(); // must be explicitly closed to ensure memory stream can be read
            var encryptedText = memoryStream.ToArray();

            // encrypted output deliberately includes salt and initialization vector to facilitate subsequent decryption
            return Convert.ToBase64String(salt.Concat(aes.IV).Concat(encryptedText).ToArray());
        }

        public static string Decrypt(string cipherText, string password)
        {
            // get the constituent parts from the cipher text
            var cipherArray = Convert.FromBase64String(cipherText);
            var salt = cipherArray.Take(KeySize).ToArray();
            var initializationVector = cipherArray.Skip(KeySize).Take(IvSize).ToArray();
            var encryptedText = cipherArray.Skip(KeySize + IvSize).ToArray();

            // re-generate the derived key from the password and known salt & iteration count
            var derivedKey = GetDerivedKey(password, salt);

            // decrypt - using the derived key (not the password)
            using var aes = Aes.Create();
            using var cryptoTransform = aes.CreateDecryptor(derivedKey, initializationVector);
            using var memoryStream = new MemoryStream(encryptedText);
            using var cryptoStream = new CryptoStream(memoryStream, cryptoTransform, CryptoStreamMode.Read);
            using var streamReader = new StreamReader(cryptoStream);
            var plainText = streamReader.ReadToEnd();
            streamReader.Close();

            return plainText;
        }

        private static byte[] GetDerivedKey(string password, byte[] salt)
        {
            using var keyGenerator = new Rfc2898DeriveBytes(password, salt, KeyIterationCount);
            return keyGenerator.GetBytes(KeySize);
        }

        private const int KeySize = 32; // AES key size is limited to either 128 bit (16 byte) or 256 bit (32 byte)
        private const int IvSize = 16;  // AES IV size is fixed at 128 bit (16 bytes)
        private const int KeyIterationCount = 5000;
    }
}