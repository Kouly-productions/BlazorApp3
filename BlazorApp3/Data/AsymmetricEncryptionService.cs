using System;
using System.Security.Cryptography;
using System.Text;

namespace BlazorApp3.Services
{
    public interface IAsymmetricEncryptionService
    {
        string PrivateKey { get; }
        string PublicKey { get; }
        string Decrypt(string encryptedData);
        void GenerateNewKeyPair();
        bool LoadKeys(string publicKey, string privateKey);
        void SaveKeys();
    }

    public class AsymmetricEncryptionService : IAsymmetricEncryptionService
    {
        private RSA _rsaProvider;
        private readonly IConfiguration _configuration;

        public string PrivateKey { get; private set; }
        public string PublicKey { get; private set; }

        public AsymmetricEncryptionService(IConfiguration configuration)
        {
            _configuration = configuration;
            _rsaProvider = RSA.Create(2048);

            // Try to load keys from configuration
            string storedPublicKey = _configuration["Encryption:RSA:PublicKey"];
            string storedPrivateKey = _configuration["Encryption:RSA:PrivateKey"];

            if (!string.IsNullOrEmpty(storedPublicKey) && !string.IsNullOrEmpty(storedPrivateKey))
            {
                bool success = LoadKeys(storedPublicKey, storedPrivateKey);
                if (!success)
                {
                    Console.WriteLine("Failed to load RSA keys from configuration. Generating new keys.");
                    GenerateNewKeyPair();
                    SaveKeys();
                }
                else
                {
                    Console.WriteLine("Successfully loaded RSA keys from configuration.");
                }
            }
            else
            {
                Console.WriteLine("No RSA keys found in configuration. Generating new keys.");
                GenerateNewKeyPair();
                SaveKeys();
            }

            // Verify the keys work correctly
            try
            {
                string testData = "Test encryption";
                byte[] testBytes = Encoding.UTF8.GetBytes(testData);
                byte[] encrypted = _rsaProvider.Encrypt(testBytes, RSAEncryptionPadding.OaepSHA256);
                byte[] decrypted = _rsaProvider.Decrypt(encrypted, RSAEncryptionPadding.OaepSHA256);
                string result = Encoding.UTF8.GetString(decrypted);
                Console.WriteLine($"RSA key verification successful. Test: '{testData}' => '{result}'");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RSA key verification failed: {ex.Message}");
                Console.WriteLine("Generating new key pair due to verification failure");
                GenerateNewKeyPair();
                SaveKeys();
            }
        }

        public void GenerateNewKeyPair()
        {
            _rsaProvider = RSA.Create(2048);
            PublicKey = Convert.ToBase64String(_rsaProvider.ExportRSAPublicKey());
            PrivateKey = Convert.ToBase64String(_rsaProvider.ExportRSAPrivateKey());
            Console.WriteLine("Generated new RSA key pair");
        }

        public bool LoadKeys(string publicKey, string privateKey)
        {
            try
            {
                _rsaProvider = RSA.Create();
                _rsaProvider.ImportRSAPrivateKey(Convert.FromBase64String(privateKey), out _);
                _rsaProvider.ImportRSAPublicKey(Convert.FromBase64String(publicKey), out _);

                PublicKey = publicKey;
                PrivateKey = privateKey;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading RSA keys: {ex.Message}");
                GenerateNewKeyPair();
                return false;
            }
        }

        public void SaveKeys()
        {
            try
            {
                // In a real application, you would use a more secure storage method
                // like Azure Key Vault, but for this exercise we'll use appsettings.json
                var configBuilder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

                var config = configBuilder.Build();

                // Update the configuration
                var settings = new Dictionary<string, string>
                {
                    {"Encryption:RSA:PublicKey", PublicKey},
                    {"Encryption:RSA:PrivateKey", PrivateKey}
                };

                // Write to appsettings.json
                // Note: In production, you should use a more secure method
                var appSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
                string json = File.ReadAllText(appSettingsPath);
                dynamic jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject(json);

                if (jsonObj["Encryption"] == null)
                {
                    jsonObj["Encryption"] = new Newtonsoft.Json.Linq.JObject();
                }

                if (jsonObj["Encryption"]["RSA"] == null)
                {
                    jsonObj["Encryption"]["RSA"] = new Newtonsoft.Json.Linq.JObject();
                }

                jsonObj["Encryption"]["RSA"]["PublicKey"] = PublicKey;
                jsonObj["Encryption"]["RSA"]["PrivateKey"] = PrivateKey;

                string output = Newtonsoft.Json.JsonConvert.SerializeObject(jsonObj, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(appSettingsPath, output);

                Console.WriteLine("Saved RSA keys to configuration");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving RSA keys: {ex.Message}");
            }
        }

        public string Decrypt(string encryptedData)
        {
            try
            {
                Console.WriteLine($"Attempting to decrypt data starting with: {encryptedData.Substring(0, Math.Min(20, encryptedData.Length))}...");

                byte[] dataBytes = Convert.FromBase64String(encryptedData);
                byte[] decryptedBytes = _rsaProvider.Decrypt(dataBytes, RSAEncryptionPadding.OaepSHA256);
                string result = Encoding.UTF8.GetString(decryptedBytes);

                Console.WriteLine($"Decryption successful: {result.Substring(0, Math.Min(20, result.Length))}...");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Decryption error: {ex.Message}");
                Console.WriteLine($"Encrypted data length: {encryptedData.Length}");
                return string.Empty;
            }
        }
    }
}