using System;
using System.Security.Cryptography;
using System.Text;

namespace BlazorApp3.Services
{
    public interface IAsymmetricEncryptionService
    {
        string PrivateKey { get; }
        string PublicKey { get; }
        string DbPrivateKey { get; }
        string DbPublicKey { get; }

        string Decrypt(string encryptedData);
        void GenerateNewKeyPair();
        bool LoadKeys(string publicKey, string privateKey);
        void SaveKeys();

        string DecryptDatabaseData(string encryptedData);
        string DecryptDatabaseDataWithKey(string encryptedData, string privateKey);
        string DecryptCSharp(string encryptedData, string privateKey);
    }

    public class AsymmetricEncryptionService : IAsymmetricEncryptionService
    {
        private RSA _rsaProvider;
        private RSA _dbRsaProvider;
        private readonly IConfiguration _configuration;

        public string PrivateKey { get; private set; }
        public string PublicKey { get; private set; }

        public string DbPrivateKey { get; private set; }
        public string DbPublicKey { get; private set; }

        public AsymmetricEncryptionService(IConfiguration configuration)
        {
            _configuration = configuration;
            _rsaProvider = RSA.Create(2048);
            _dbRsaProvider = RSA.Create(2048);

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

            // Load database keys
            string storedDbPublicKey = _configuration["DatabaseEncryption:DbRSA:PublicDbKey"]!;
            string storedDbPrivateKey = _configuration["DatabaseEncryption:DbRSA:PrivateDbKey"]!;

            // Set the database keys to the properties
            if (!string.IsNullOrEmpty(storedDbPublicKey) && !string.IsNullOrEmpty(storedDbPrivateKey))
            {
                DbPublicKey = storedDbPublicKey;  // Set the public DB key
                DbPrivateKey = storedDbPrivateKey; // Set the private DB key

                // Initialize the database RSA provider
                LoadDatabaseKeys(DbPublicKey, DbPrivateKey);
            }

        }

        public void GenerateNewKeyPair()
        {
            _rsaProvider = RSA.Create(2048);
            PublicKey = Convert.ToBase64String(_rsaProvider.ExportRSAPublicKey());
            PrivateKey = Convert.ToBase64String(_rsaProvider.ExportRSAPrivateKey());
            Console.WriteLine("New key pair generated.");

            SaveKeys();  // Ensure keys are saved immediately

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

        private bool LoadDatabaseKeys(string publicKey, string privateKey)
        {
            try
            {
                _dbRsaProvider = RSA.Create();
                _dbRsaProvider.ImportRSAPrivateKey(Convert.FromBase64String(privateKey), out _);
                _dbRsaProvider.ImportRSAPublicKey(Convert.FromBase64String(publicKey), out _);

                Console.WriteLine($"Public Key: {DbPublicKey}");
                Console.WriteLine($"Private Key: {DbPrivateKey}");

                PublicKey = publicKey;
                PrivateKey = privateKey;

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load database RSA keys: {ex.Message}");
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
                Console.WriteLine($"Decrypt text from asymservice {decryptedBytes}");
                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Decryption error asymservice: {ex.Message}");
                return string.Empty;
            }
        }

        public string DecryptDatabaseData(string encryptedData)
        {
            Console.WriteLine("Database Decrypt trycatch");
            try
            {
                // Check if database RSA provider is initialized
                if (_dbRsaProvider == null)
                {
                    Console.WriteLine("Database RSA provider not initialized.");
                    return string.Empty;
                }

                byte[] dataBytes = Convert.FromBase64String(encryptedData);
                byte[] decryptedBytes = _dbRsaProvider.Decrypt(dataBytes, RSAEncryptionPadding.OaepSHA256);
                Console.WriteLine($"Database decrypt successful, length: {decryptedBytes.Length} bytes");
                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database decryption error: {ex.Message}");
                return string.Empty;
            }
        }

        public string DecryptDatabaseDataWithKey(string encryptedData, string privateKeyBase64)
        {
            //RSA _dbRsaProvider = RSA.Create(2048);


            Console.WriteLine("Database Decrypt trycatch");
            try
            {
                // Import the private key
                if (string.IsNullOrWhiteSpace(privateKeyBase64))
                {
                    Console.WriteLine("Private key is null or empty.");
                    return string.Empty;
                }

                // Convert the Base64 string to bytes and import the private key
                byte[] privateKeyBytes = Convert.FromBase64String(privateKeyBase64);
                _dbRsaProvider.ImportRSAPrivateKey(privateKeyBytes, out _);

                byte[] dataBytes = Convert.FromBase64String(encryptedData);
                byte[] decryptedBytes = _dbRsaProvider.Decrypt(dataBytes, RSAEncryptionPadding.OaepSHA256);
                Console.WriteLine($"Database decrypt successful, length: {decryptedBytes.Length} bytes");
                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database decryption error: {ex.Message}");
                return string.Empty;
            }
        }

        public string DecryptCSharp(string encryptedDataParam, string privateKey)
        {
            using (RSA rsa = RSA.Create())
            {
                rsa.ImportRSAPrivateKey(Convert.FromBase64String(privateKey), out _);

                // Decrypt the data
                byte[] encryptedData = Convert.FromBase64String(encryptedDataParam);
                byte[] decryptedData = rsa.Decrypt(encryptedData, RSAEncryptionPadding.OaepSHA256);
                string decryptedText = Encoding.UTF8.GetString(decryptedData);

                return decryptedText;

            }

        }

    }
}