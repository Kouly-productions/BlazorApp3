using System;
using System.Security.Cryptography;
using System.Text;

namespace BlazorApp3.Services
{
    public interface IHashingService
    {
        T SHA2<T>(T textToHash);
        T HMAC<T>(T textToHash);
        T PBKDF2<T>(T textToHash);
        T BCrypt<T>(T textToHash);
        string HmacKey { get; set; }
        bool VerifyHash(string input, string hashedValue, string algorithm);
    }

    public class HashingService : IHashingService
    {
        public string HmacKey { get; set; } = "DefaultSecretKey123!@#";

        // SHA2 Implementation (SHA-256)
        public T SHA2<T>(T textToHash)
        {
            byte[] inputBytes = ConvertToBytes(textToHash);

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(inputBytes);
                return ConvertFromBytes<T>(hashBytes);
            }
        }

        // HMAC Implementation (HMAC-SHA256)
        public T HMAC<T>(T textToHash)
        {
            byte[] inputBytes = ConvertToBytes(textToHash);
            byte[] keyBytes = Encoding.UTF8.GetBytes(HmacKey);

            using (HMACSHA256 hmac = new HMACSHA256(keyBytes))
            {
                byte[] hashBytes = hmac.ComputeHash(inputBytes);
                return ConvertFromBytes<T>(hashBytes);
            }
        }

        // PBKDF2 Implementation
        public T PBKDF2<T>(T textToHash)
        {
            byte[] inputBytes = ConvertToBytes(textToHash);

            // Generate a random salt (enhanced entropy)
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // Create the hash with 10000 iterations (configurable) using SHA256 algorithm
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                inputBytes,
                salt,
                10000, // iterations
                HashAlgorithmName.SHA256, // explicit algorithm
                32); // output length

            // Combine salt and hash for storage
            byte[] hashBytes = new byte[48]; // 16 bytes salt + 32 bytes hash
            Array.Copy(salt, 0, hashBytes, 0, 16);
            Array.Copy(hash, 0, hashBytes, 16, 32);

            return ConvertFromBytes<T>(hashBytes);
        }

        public T BCrypt<T>(T textToHash)
        {
            // Since we can't use the actual BCrypt implementation without the package,
            // we'll use PBKDF2 with different parameters as a fallback
            byte[] inputBytes = ConvertToBytes(textToHash);

            // Generate a random salt (enhanced entropy)
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // Create the hash with higher iterations to simulate BCrypt work factor
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                inputBytes,
                salt,
                20000, // higher iterations
                HashAlgorithmName.SHA256,
                32);

            // Combine salt and hash with a marker to indicate this is a BCrypt substitute
            byte[] hashBytes = new byte[49]; // 1 byte marker + 16 bytes salt + 32 bytes hash
            hashBytes[0] = 0xBC; // Marker byte
            Array.Copy(salt, 0, hashBytes, 1, 16);
            Array.Copy(hash, 0, hashBytes, 17, 32);

            return ConvertFromBytes<T>(hashBytes);
        }

        // Unified verification method
        public bool VerifyHash(string input, string hashedValue, string algorithm)
        {
            switch (algorithm.ToUpper())
            {
                case "SHA2":
                    string computedSha2 = SHA2(input);
                    return computedSha2 == hashedValue;
                case "HMAC":
                    string computedHmac = HMAC(input);
                    return computedHmac == hashedValue;
                case "PBKDF2":
                    return VerifyPBKDF2(input, hashedValue);
                case "BCRYPT":
                    return VerifyAlternativeBCrypt(input, hashedValue);
                default:
                    throw new ArgumentException("Unsupported algorithm", nameof(algorithm));
            }
        }

        private bool VerifyAlternativeBCrypt(string input, string hashedValue)
        {
            try
            {
                // Extract marker, salt, and hash from the base64 string
                byte[] hashBytes = Convert.FromBase64String(hashedValue);

                // Check for our marker byte
                if (hashBytes[0] != 0xBC || hashBytes.Length != 49)
                {
                    return false;
                }

                // Extract the salt (next 16 bytes after marker)
                byte[] salt = new byte[16];
                Array.Copy(hashBytes, 1, salt, 0, 16);

                // Hash the input with the extracted salt
                byte[] computedHash = Rfc2898DeriveBytes.Pbkdf2(
                    Encoding.UTF8.GetBytes(input),
                    salt,
                    20000,
                    HashAlgorithmName.SHA256,
                    32);

                // Compare the computed hash with the stored hash
                for (int i = 0; i < 32; i++)
                {
                    if (hashBytes[i + 17] != computedHash[i])
                        return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        // Helper method for PBKDF2 verification
        private bool VerifyPBKDF2(string input, string hashedValue)
        {
            try
            {
                // Extract salt from base64 string
                byte[] hashBytes = Convert.FromBase64String(hashedValue);

                // Extract the salt (first 16 bytes)
                byte[] salt = new byte[16];
                Array.Copy(hashBytes, 0, salt, 0, 16);

                // Hash the input with the extracted salt
                byte[] computedHash = Rfc2898DeriveBytes.Pbkdf2(
                    Encoding.UTF8.GetBytes(input),
                    salt,
                    10000,
                    HashAlgorithmName.SHA256,
                    32);

                // Compare the computed hash with the stored hash
                for (int i = 0; i < 32; i++)
                {
                    if (hashBytes[i + 16] != computedHash[i])
                        return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        // Helper methods for type conversion
        private byte[] ConvertToBytes<T>(T value)
        {
            if (value is string strValue)
            {
                return Encoding.UTF8.GetBytes(strValue);
            }
            else if (value is byte[] byteValue)
            {
                return byteValue;
            }
            else
            {
                throw new ArgumentException("Input must be either string or byte[]");
            }
        }

        private T ConvertFromBytes<T>(byte[] bytes)
        {
            if (typeof(T) == typeof(string))
            {
                return (T)(object)Convert.ToBase64String(bytes);
            }
            else if (typeof(T) == typeof(byte[]))
            {
                return (T)(object)bytes;
            }
            else
            {
                throw new ArgumentException("Output type must be either string or byte[]");
            }
        }
    }
}