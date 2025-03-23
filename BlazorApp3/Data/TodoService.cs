using BlazorApp3.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Text;

namespace BlazorApp3.Services
{
    public interface ITodoService
    {
        Task<List<TodoItem>> GetUserTodoItemsAsync(string userId);
        Task<TodoItem> GetTodoItemAsync(int id);
        Task<TodoItem> CreateTodoItemAsync(TodoItem item);
        Task<TodoItem> UpdateTodoItemAsync(TodoItem item);
        Task<bool> DeleteTodoItemAsync(int id);
    }

    public class TodoService : ITodoService
    {
        private readonly TodoDbContext _dbContext;
        private readonly IAsymmetricEncryptionService _encryptionService;
        private readonly IApiClientService _apiClient;
        private readonly ILogger<TodoService> _logger;

        public TodoService(
            TodoDbContext dbContext,
            IAsymmetricEncryptionService encryptionService,
            IApiClientService apiClient,
            ILogger<TodoService> logger = null)
        {
            _dbContext = dbContext;
            _encryptionService = encryptionService;
            _apiClient = apiClient;
            _logger = logger;
        }

        public async Task<List<TodoItem>> GetUserTodoItemsAsync(string userId)
        {
            // Use AsNoTracking() to prevent EF from tracking these entities
            var items = await _dbContext.TodoItems
                .AsNoTracking()
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedDate)
                .ToListAsync();

            // Create a new collection to hold our display items
            var displayItems = new List<TodoItem>();

            // Try to decrypt all items
            foreach (var item in items)
            {
                // Create a copy of the item for display purposes
                var displayItem = new TodoItem
                {
                    Id = item.Id,
                    UserId = item.UserId,
                    Title = item.Title,
                    Description = item.Description,
                    IsCompleted = item.IsCompleted,
                    IsEncrypted = item.IsEncrypted,
                    CreatedDate = item.CreatedDate
                };

                if (displayItem.IsEncrypted)
                {
                    try
                    {
                        // Decrypt the title
                        string decryptedTitle = _encryptionService.DecryptDatabaseDataWithKey(displayItem.Title, "MIIEpAIBAAKCAQEA0nstMYkSCp3UhFRlzC6sfrxRdJz/Wjc4S6tLAGbVYz6OnY6Z37lsDxsURgbEMcmxLdyerQDSkA6IqCxRctaqOgNgYIjObK+G/fLT+JfJE9Bbx4mTPuxkBz968DoRPsPARHuHqi4ID2cCGKlK3Q3eOXZBVuHkRYWnJSwo/YYXMb7Z6U7+LHRGWhZ4n4PEK0MdYmgZJQJmeEIjpxMTdo5LzfLQMK9my1gmXB8sBobxipqOEnaHkHWrpgeC+eWTld+VRCorwhkyx3tMtXVGaJnzjLuUIkBsq9uTvtA0pvPdPrzurRGDNqau2hyeh6UJ3TcZrSfI6h+zhDbG05liSYeLzQIDAQABAoIBAHvMjWlsWNs7t+rZhUKSVUz50ONJEHxsrET9jFDBK1ODUPjlDiZj9mXwJH2Hr1AldHwoHoBdUnv+wGxCHOnxzw/uOnqtHNUabMjjUcAO7usji0gS6DODcNY+hT3UuZ30HxtomQQErSL6EGaW9HyOkp2zq3zBwrUNhqE7lyR/ARd1OzcJLeTxAanBYwF1TtuOzh5AYvFeG4C1oYGTtRHu0vanKlx5zpF9nk4iGgu1FtxdJ+tmdKLiN7tWDtKrI5ooD1+FNQRek21PTjULcGuqaCAAfLIFxV0BuptsFKV0Tu1pbg4eQA7s+qBAAB/VrWLX02KLw61/D3iVS2/qKX6itFUCgYEA2g5P3Rq4F7vshDr9jJUhwxu19+Vj6MEsuaWdpnvcig27jRFJYOwgMJyI8VZZNQ5mxzGPjY77koRVy/EeC9AJOuQlxl4cjpfjw9F6SeAKp6+Qvb8mYlKD75YswAKDaKfdAscUfRaIHJ3gxsPln9nT/WZzqAqj3GIFEFSBCHqxFi8CgYEA9xtvGCfq4RpSVA+RQmmdw+Q3U0q6xTWZM5RO9kNG92Hh/sRHkN8GVoNQmezFwCQ4DUxvTReUeR0jw5TX7rQWZ4A+Rw5iSyd2/u+yqrgh0s+wLPqZQeyk/o7vhiNyW+LDBbdFl3RiHZtdi25ot8Wnx4ZaMPMfp0rcLh+wjwnzOsMCgYEAy9QnCUppnR34N56g1eGtfqEPlxshKjgwo5TRagdMHuw5TeND0UrHyEj6pYWOu86rejW0t6FZPhtfy9SmvmoHxrnvKZ9dWFlY+fl9M0MvEpJFXWkp6yyw0atyR0XSKmkHagpH96mxL/bQX1xM8ACBbdRv9juD8oTZsOsc9p0hndcCgYEA66QdrMtkEIUpPUAbJVnSOJvIpoT81lLmZWloYy6E3iNZf7ltBZmoUZenpSFE8pWXXhcljD6QN26yTDAEOn1BYDHLMbdlxIU91J5/oo00s/OZ7UqMG3GvZZComSH0S+tSToEWu/cgGVuvOOdwtM6n0H0uRL+Tz9RzYwiVNdInQEECgYB/CI9PV+shV3hPHClcpXPKXHM6pFO44HWoaZ4HhkR3WU73qZio6lUZDtjxps+kdb/hiL8FhihswG4oHa67sSOGvIZmywx7Fz+1LGV5mKmLALvGSEMLXNeTv3PGbf3dHT1d52ndlZywU5/hi7Q1jUJJbbFXvAmJZdd4UI3INOk1Ug==");
                        if (!string.IsNullOrEmpty(decryptedTitle))
                        {
                            LogDebug($"Successfully decrypted title for item {displayItem.Id}");
                            displayItem.Title = decryptedTitle;
                        }

                        // Decrypt the description if it exists
                        if (!string.IsNullOrEmpty(displayItem.Description))
                        {
                            string decryptedDesc = _encryptionService.DecryptDatabaseDataWithKey(displayItem.Description, "MIIEpAIBAAKCAQEA0nstMYkSCp3UhFRlzC6sfrxRdJz/Wjc4S6tLAGbVYz6OnY6Z37lsDxsURgbEMcmxLdyerQDSkA6IqCxRctaqOgNgYIjObK+G/fLT+JfJE9Bbx4mTPuxkBz968DoRPsPARHuHqi4ID2cCGKlK3Q3eOXZBVuHkRYWnJSwo/YYXMb7Z6U7+LHRGWhZ4n4PEK0MdYmgZJQJmeEIjpxMTdo5LzfLQMK9my1gmXB8sBobxipqOEnaHkHWrpgeC+eWTld+VRCorwhkyx3tMtXVGaJnzjLuUIkBsq9uTvtA0pvPdPrzurRGDNqau2hyeh6UJ3TcZrSfI6h+zhDbG05liSYeLzQIDAQABAoIBAHvMjWlsWNs7t+rZhUKSVUz50ONJEHxsrET9jFDBK1ODUPjlDiZj9mXwJH2Hr1AldHwoHoBdUnv+wGxCHOnxzw/uOnqtHNUabMjjUcAO7usji0gS6DODcNY+hT3UuZ30HxtomQQErSL6EGaW9HyOkp2zq3zBwrUNhqE7lyR/ARd1OzcJLeTxAanBYwF1TtuOzh5AYvFeG4C1oYGTtRHu0vanKlx5zpF9nk4iGgu1FtxdJ+tmdKLiN7tWDtKrI5ooD1+FNQRek21PTjULcGuqaCAAfLIFxV0BuptsFKV0Tu1pbg4eQA7s+qBAAB/VrWLX02KLw61/D3iVS2/qKX6itFUCgYEA2g5P3Rq4F7vshDr9jJUhwxu19+Vj6MEsuaWdpnvcig27jRFJYOwgMJyI8VZZNQ5mxzGPjY77koRVy/EeC9AJOuQlxl4cjpfjw9F6SeAKp6+Qvb8mYlKD75YswAKDaKfdAscUfRaIHJ3gxsPln9nT/WZzqAqj3GIFEFSBCHqxFi8CgYEA9xtvGCfq4RpSVA+RQmmdw+Q3U0q6xTWZM5RO9kNG92Hh/sRHkN8GVoNQmezFwCQ4DUxvTReUeR0jw5TX7rQWZ4A+Rw5iSyd2/u+yqrgh0s+wLPqZQeyk/o7vhiNyW+LDBbdFl3RiHZtdi25ot8Wnx4ZaMPMfp0rcLh+wjwnzOsMCgYEAy9QnCUppnR34N56g1eGtfqEPlxshKjgwo5TRagdMHuw5TeND0UrHyEj6pYWOu86rejW0t6FZPhtfy9SmvmoHxrnvKZ9dWFlY+fl9M0MvEpJFXWkp6yyw0atyR0XSKmkHagpH96mxL/bQX1xM8ACBbdRv9juD8oTZsOsc9p0hndcCgYEA66QdrMtkEIUpPUAbJVnSOJvIpoT81lLmZWloYy6E3iNZf7ltBZmoUZenpSFE8pWXXhcljD6QN26yTDAEOn1BYDHLMbdlxIU91J5/oo00s/OZ7UqMG3GvZZComSH0S+tSToEWu/cgGVuvOOdwtM6n0H0uRL+Tz9RzYwiVNdInQEECgYB/CI9PV+shV3hPHClcpXPKXHM6pFO44HWoaZ4HhkR3WU73qZio6lUZDtjxps+kdb/hiL8FhihswG4oHa67sSOGvIZmywx7Fz+1LGV5mKmLALvGSEMLXNeTv3PGbf3dHT1d52ndlZywU5/hi7Q1jUJJbbFXvAmJZdd4UI3INOk1Ug==");
                            if (!string.IsNullOrEmpty(decryptedDesc))
                            {
                                displayItem.Description = decryptedDesc;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError($"Decryption error for item {displayItem.Id}: {ex.Message}");
                        // Decryption failed, leave as is
                    }
                }

                displayItems.Add(displayItem);
            }

            return displayItems;
        }

        public async Task<TodoItem> GetTodoItemAsync(int id)
        {
            var item = await _dbContext.TodoItems.FindAsync(id);

            if (item != null && item.IsEncrypted)
            {
                try
                {
                    // Decrypt the title
                    string decryptedTitle = _encryptionService.Decrypt(item.Title);
                    if (!string.IsNullOrEmpty(decryptedTitle))
                    {
                        item.Title = decryptedTitle;
                    }

                    // Decrypt the description if it exists
                    if (!string.IsNullOrEmpty(item.Description))
                    {
                        string decryptedDesc = _encryptionService.Decrypt(item.Description);
                        if (!string.IsNullOrEmpty(decryptedDesc))
                        {
                            item.Description = decryptedDesc;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Decryption error for item {item.Id}: {ex.Message}");
                    // Decryption failed, leave as is
                }
            }

            return item;
        }

        public async Task<TodoItem> CreateTodoItemAsync(TodoItem item)
        {
            try
            {
                // Debug output
                LogDebug($"Encrypting new item with title: {item.Title}");
                LogDebug($"Using public key beginning with: {_encryptionService.PublicKey.Substring(0, Math.Min(20, _encryptionService.PublicKey.Length))}...");

                // Encrypt the title and description
                string encryptedTitle = await _apiClient.EncryptDatabaseApiAsync(item.Title);
                string encryptedDescription = !string.IsNullOrEmpty(item.Description)
                    ? await _apiClient.EncryptDatabaseApiAsync(item.Description)
                    : string.Empty;

                // If encryption was successful, store the encrypted values
                if (!string.IsNullOrEmpty(encryptedTitle))
                {
                    LogDebug($"Successfully encrypted title to: {encryptedTitle.Substring(0, Math.Min(20, encryptedTitle.Length))}...");
                    item.Title = encryptedTitle;
                    item.Description = encryptedDescription;
                    item.IsEncrypted = true;
                }
                else
                {
                    LogDebug("Encryption failed - API returned empty result");
                }

                _dbContext.TodoItems.Add(item);
                await _dbContext.SaveChangesAsync();
                return item;
            }
            catch (Exception ex)
            {
                // If there's an error with encryption, save without encryption
                LogError($"Error encrypting data: {ex.Message}");

                // Reset to original values
                item.IsEncrypted = false;

                _dbContext.TodoItems.Add(item);
                await _dbContext.SaveChangesAsync();
                return item;
            }
        }

        public async Task<TodoItem> UpdateTodoItemAsync(TodoItem item)
        {
            var existingItem = await _dbContext.TodoItems.FindAsync(item.Id);
            if (existingItem == null)
                return null;

            try
            {
                // Always encrypt when updating - consistency is better
                string encryptedTitle = await _apiClient.EncryptWithApiAsync(item.Title);
                string encryptedDescription = !string.IsNullOrEmpty(item.Description)
                    ? await _apiClient.EncryptWithApiAsync(item.Description)
                    : string.Empty;

                if (!string.IsNullOrEmpty(encryptedTitle))
                {
                    existingItem.Title = encryptedTitle;
                    existingItem.Description = encryptedDescription;
                    existingItem.IsEncrypted = true;
                }
                else
                {
                    // If encryption fails, update without encryption
                    existingItem.Title = item.Title;
                    existingItem.Description = item.Description;
                    existingItem.IsEncrypted = false;
                }
            }
            catch (Exception ex)
            {
                // If encryption fails, update without encryption
                LogError($"Error updating with encryption: {ex.Message}");
                existingItem.Title = item.Title;
                existingItem.Description = item.Description;
                existingItem.IsEncrypted = false;
            }

            existingItem.IsCompleted = item.IsCompleted;

            _dbContext.TodoItems.Update(existingItem);
            await _dbContext.SaveChangesAsync();
            return existingItem;
        }

        public async Task<bool> DeleteTodoItemAsync(int id)
        {
            var item = await _dbContext.TodoItems.FindAsync(id);
            if (item == null)
                return false;

            _dbContext.TodoItems.Remove(item);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        // Helper logging methods
        private void LogDebug(string message)
        {
            Console.WriteLine($"DEBUG: {message}");
            _logger?.LogDebug(message);
        }

        private void LogError(string message)
        {
            Console.WriteLine($"ERROR: {message}");
            _logger?.LogError(message);
        }
    }
}