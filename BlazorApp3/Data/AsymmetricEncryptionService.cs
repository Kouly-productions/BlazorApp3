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
                LoadKeys(storedPublicKey, storedPrivateKey);
            }
            else
            {
                GenerateNewKeyPair();
                SaveKeys();
            }
        }

        public void GenerateNewKeyPair()
        {
            _rsaProvider = RSA.Create(2048);
            PublicKey = Convert.ToBase64String(_rsaProvider.ExportRSAPublicKey());
            PrivateKey = Convert.ToBase64String(_rsaProvider.ExportRSAPrivateKey());
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
            catch
            {
                GenerateNewKeyPair();
                return false;
            }
        }

        public void SaveKeys()
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
        }

        public string Decrypt(string encryptedData)
        {
            try
            {
                byte[] dataBytes = Convert.FromBase64String(encryptedData);
                byte[] decryptedBytes = _rsaProvider.Decrypt(dataBytes, RSAEncryptionPadding.OaepSHA256);
                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Decryption error: {ex.Message}");
                return string.Empty;
            }
        }
    }
}