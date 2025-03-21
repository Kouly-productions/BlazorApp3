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
            var items = await _dbContext.TodoItems
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedDate)
                .ToListAsync();

            // Try to decrypt all items
            foreach (var item in items)
            {
                if (item.IsEncrypted)
                {
                    try
                    {
                        // Debug the encrypted data
                        LogDebug($"Attempting to decrypt item {item.Id}, encrypted title: {item.Title.Substring(0, Math.Min(20, item.Title.Length))}...");
                        LogDebug($"Using private key beginning with: {_encryptionService.PrivateKey.Substring(0, Math.Min(20, _encryptionService.PrivateKey.Length))}...");

                        // Decrypt the title
                        string decryptedTitle = _encryptionService.Decrypt(item.Title);
                        if (!string.IsNullOrEmpty(decryptedTitle))
                        {
                            LogDebug($"Successfully decrypted title: {decryptedTitle.Substring(0, Math.Min(20, decryptedTitle.Length))}...");
                            item.Title = decryptedTitle;
                        }
                        else
                        {
                            LogDebug("Decryption returned empty result");
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
            }

            return items;
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
                string encryptedTitle = await _apiClient.EncryptWithApiAsync(item.Title);
                string encryptedDescription = !string.IsNullOrEmpty(item.Description)
                    ? await _apiClient.EncryptWithApiAsync(item.Description)
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