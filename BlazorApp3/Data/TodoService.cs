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

            // Decrypt any encrypted items
            foreach (var item in items)
            {
                if (item.IsEncrypted)
                {
                    item.Title = _encryptionService.Decrypt(item.Title);
                    if (!string.IsNullOrEmpty(item.Description))
                    {
                        item.Description = _encryptionService.Decrypt(item.Description);
                    }
                    // Don't change the IsEncrypted flag here so we know to encrypt again if updated
                }
            }

            return items;
        }

        public async Task<TodoItem> GetTodoItemAsync(int id)
        {
            var item = await _dbContext.TodoItems.FindAsync(id);

            if (item != null && item.IsEncrypted)
            {
                item.Title = _encryptionService.Decrypt(item.Title);
                if (!string.IsNullOrEmpty(item.Description))
                {
                    item.Description = _encryptionService.Decrypt(item.Description);
                }
            }

            return item;
        }

        public async Task<TodoItem> CreateTodoItemAsync(TodoItem item)
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
                item.IsEncrypted = true;
            }

            _dbContext.TodoItems.Add(item);
            await _dbContext.SaveChangesAsync();
            return item;
        }

        public async Task<TodoItem> UpdateTodoItemAsync(TodoItem item)
        {
            var existingItem = await _dbContext.TodoItems.FindAsync(item.Id);
            if (existingItem == null)
                return null;

            // Re-encrypt if needed
            if (existingItem.IsEncrypted)
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
    }
}