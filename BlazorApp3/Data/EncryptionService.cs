using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace BlazorApp3.Services
{
    public interface IEncryptionService
    {
        string Encrypt(string plainText);
        string Decrypt(string cipherText);
        string PrivateKey { get; set; }
    }

    public class EncryptionService : IEncryptionService
    {
        // Private key for encryption - use a secure key in production
        public string PrivateKey { get; set; } = "E546C8DF278CD5931069B522E695D4F2"; // 32 chars = 128 bits

        // Initialization Vector - this should be unique for each encryption operation in production
        private readonly byte[] _initializationVector = new byte[]
        {
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0x09, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16
        };

        /// <summary>
        /// Encrypts the plaintext using AES encryption
        /// </summary>
        /// <param name="plainText">Text to encrypt</param>
        /// <returns>Base64 encoded encrypted string</returns>
        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] keyBytes = Encoding.UTF8.GetBytes(PrivateKey);

            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.IV = _initializationVector;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                        cryptoStream.FlushFinalBlock();
                    }

                    byte[] cipherTextBytes = memoryStream.ToArray();
                    return Convert.ToBase64String(cipherTextBytes);
                }
            }
        }

        /// <summary>
        /// Decrypts the ciphertext using AES decryption
        /// </summary>
        /// <param name="cipherText">Base64 encoded encrypted string</param>
        /// <returns>Decrypted plaintext</returns>
        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return string.Empty;

            try
            {
                byte[] cipherTextBytes = Convert.FromBase64String(cipherText);
                byte[] keyBytes = Encoding.UTF8.GetBytes(PrivateKey);

                using (Aes aes = Aes.Create())
                {
                    aes.Key = keyBytes;
                    aes.IV = _initializationVector;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (ICryptoTransform decryptor = aes.CreateDecryptor())
                    using (MemoryStream memoryStream = new MemoryStream(cipherTextBytes))
                    {
                        using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader streamReader = new StreamReader(cryptoStream))
                            {
                                return streamReader.ReadToEnd();
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Return empty string or handle error appropriately
                return string.Empty;
            }
        }
    }
}