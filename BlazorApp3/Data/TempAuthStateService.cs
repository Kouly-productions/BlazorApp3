using System;
using System.Collections.Concurrent;

namespace BlazorApp3.Services
{
    // Make the interface partial so we can extend it
    public partial interface ITempAuthStateService
    {
        string CreatePendingAuth(string userId, bool rememberMe, string? returnUrl);
        bool TryGetPendingAuth(string token, out PendingAuth? pendingAuth);
        void RemovePendingAuth(string token);

        // Add these new methods for registration
        string CreatePendingRegistration(string email, string password, string role, string returnUrl);
        bool TryGetPendingRegistration(string token, out PendingRegistration pendingRegistration);
        void RemovePendingRegistration(string token);
    }

    public class PendingAuth
    {
        public string UserId { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
        public string? ReturnUrl { get; set; }
        public DateTime Created { get; set; }
    }

    // Add this new class for registration data
    public class PendingRegistration
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    // Make the class partial too
    public partial class TempAuthStateService : ITempAuthStateService
    {
        private readonly ConcurrentDictionary<string, PendingAuth> _pendingAuths = new();
        private readonly ConcurrentDictionary<string, PendingRegistration> _pendingRegistrations = new();
        private readonly TimeSpan _expiryTime = TimeSpan.FromMinutes(5);

        public string CreatePendingAuth(string userId, bool rememberMe, string? returnUrl)
        {
            string token = Guid.NewGuid().ToString("N");
            _pendingAuths[token] = new PendingAuth
            {
                UserId = userId,
                RememberMe = rememberMe,
                ReturnUrl = returnUrl,
                Created = DateTime.UtcNow
            };
            CleanExpiredEntries();
            return token;
        }

        public bool TryGetPendingAuth(string token, out PendingAuth? pendingAuth)
        {
            pendingAuth = null;
            if (_pendingAuths.TryGetValue(token, out var auth))
            {
                if (DateTime.UtcNow - auth.Created > _expiryTime)
                {
                    _pendingAuths.TryRemove(token, out _);
                    return false;
                }
                pendingAuth = auth;
                return true;
            }
            return false;
        }

        public void RemovePendingAuth(string token)
        {
            _pendingAuths.TryRemove(token, out _);
        }

        private void CleanExpiredEntries()
        {
            var now = DateTime.UtcNow;
            foreach (var entry in _pendingAuths)
            {
                if (now - entry.Value.Created > _expiryTime)
                {
                    _pendingAuths.TryRemove(entry.Key, out _);
                }
            }

            // Also clean up expired registration entries
            foreach (var entry in _pendingRegistrations)
            {
                if (now - entry.Value.CreatedAt > _expiryTime)
                {
                    _pendingRegistrations.TryRemove(entry.Key, out _);
                }
            }
        }

        // Implementation of new registration methods
        public string CreatePendingRegistration(string email, string password, string role, string returnUrl)
        {
            string token = Guid.NewGuid().ToString("N");
            _pendingRegistrations[token] = new PendingRegistration
            {
                Email = email,
                Password = password,
                Role = role,
                ReturnUrl = returnUrl,
                CreatedAt = DateTime.UtcNow
            };
            CleanExpiredEntries();
            return token;
        }

        public bool TryGetPendingRegistration(string token, out PendingRegistration pendingRegistration)
        {
            pendingRegistration = null;
            if (_pendingRegistrations.TryGetValue(token, out var registration))
            {
                if (DateTime.UtcNow - registration.CreatedAt > _expiryTime)
                {
                    _pendingRegistrations.TryRemove(token, out _);
                    return false;
                }
                pendingRegistration = registration;
                return true;
            }
            return false;
        }

        public void RemovePendingRegistration(string token)
        {
            _pendingRegistrations.TryRemove(token, out _);
        }
    }
}