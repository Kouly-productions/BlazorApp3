using BlazorApp3.Data;
using Microsoft.EntityFrameworkCore;

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

        public TodoService(
            TodoDbContext dbContext,
            IAsymmetricEncryptionService encryptionService,
            IApiClientService apiClient)
        {
            _dbContext = dbContext;
            _encryptionService = encryptionService;
            _apiClient = apiClient;
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
                try
                {
                    // Check if this item appears to be encrypted
                    // Simple heuristic: encrypted data is typically longer and contains non-ASCII characters
                    if (item.Title.Length > 100 && !IsAscii(item.Title))
                    {
                        string decryptedTitle = _encryptionService.Decrypt(item.Title);
                        if (!string.IsNullOrEmpty(decryptedTitle))
                        {
                            item.Title = decryptedTitle;
                        }
                    }

                    if (!string.IsNullOrEmpty(item.Description) && item.Description.Length > 100 && !IsAscii(item.Description))
                    {
                        string decryptedDesc = _encryptionService.Decrypt(item.Description);
                        if (!string.IsNullOrEmpty(decryptedDesc))
                        {
                            item.Description = decryptedDesc;
                        }
                    }
                }
                catch
                {
                    // Item wasn't encrypted or decryption failed, leave as is
                }
            }

            return items;
        }

        public async Task<TodoItem> GetTodoItemAsync(int id)
        {
            var item = await _dbContext.TodoItems.FindAsync(id);

            if (item != null)
            {
                try
                {
                    // Check if this item appears to be encrypted
                    if (item.Title.Length > 100 && !IsAscii(item.Title))
                    {
                        string decryptedTitle = _encryptionService.Decrypt(item.Title);
                        if (!string.IsNullOrEmpty(decryptedTitle))
                        {
                            item.Title = decryptedTitle;
                        }
                    }

                    if (!string.IsNullOrEmpty(item.Description) && item.Description.Length > 100 && !IsAscii(item.Description))
                    {
                        string decryptedDesc = _encryptionService.Decrypt(item.Description);
                        if (!string.IsNullOrEmpty(decryptedDesc))
                        {
                            item.Description = decryptedDesc;
                        }
                    }
                }
                catch
                {
                    // Decryption failed, leave as is
                }
            }

            return item;
        }

        public async Task<TodoItem> CreateTodoItemAsync(TodoItem item)
        {
            try
            {
                // Encrypt the title and description
                string encryptedTitle = await _apiClient.EncryptWithApiAsync(item.Title);
                string encryptedDescription = !string.IsNullOrEmpty(item.Description)
                    ? await _apiClient.EncryptWithApiAsync(item.Description)
                    : string.Empty;

                // If encryption was successful, store the encrypted values
                if (!string.IsNullOrEmpty(encryptedTitle))
                {
                    item.Title = encryptedTitle;
                    item.Description = encryptedDescription;
                    // Remove this line that uses IsEncrypted
                    // item.IsEncrypted = true;
                }

                _dbContext.TodoItems.Add(item);
                await _dbContext.SaveChangesAsync();
                return item;
            }
            catch (Exception ex)
            {
                // If there's an error with encryption, save without encryption
                Console.WriteLine($"Error encrypting data: {ex.Message}");

                // Reset to original values
                item.Title = item.Title;
                item.Description = item.Description ?? "";

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
                // Check if this item appears to be encrypted already
                bool seemsEncrypted = existingItem.Title.Length > 100 && !IsAscii(existingItem.Title);

                if (seemsEncrypted)
                {
                    string encryptedTitle = await _apiClient.EncryptWithApiAsync(item.Title);
                    string encryptedDescription = !string.IsNullOrEmpty(item.Description)
                        ? await _apiClient.EncryptWithApiAsync(item.Description)
                        : string.Empty;

                    if (!string.IsNullOrEmpty(encryptedTitle))
                    {
                        existingItem.Title = encryptedTitle;
                        existingItem.Description = encryptedDescription;
                    }
                }
                else
                {
                    existingItem.Title = item.Title;
                    existingItem.Description = item.Description;
                }
            }
            catch
            {
                // If encryption fails, update without encryption
                existingItem.Title = item.Title;
                existingItem.Description = item.Description;
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

        // Helper method to check if a string is all ASCII
        private bool IsAscii(string text)
        {
            return text.All(c => c < 128);
        }
    }
}