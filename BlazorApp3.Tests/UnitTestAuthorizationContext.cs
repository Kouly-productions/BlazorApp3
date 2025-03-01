// TestAuthorizationContext.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Moq;

namespace BlazorApp3.Tests
{
    // Helper class for authentication testing
    public class UnitTestAuthorizationContext
    {
        public readonly Mock<AuthenticationStateProvider> _authStateProvider = new();
        public AuthenticationStateProvider AuthenticationStateProvider => _authStateProvider.Object;

        public void SetAuthorized(string username)
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, username)
            }, "TestAuthType"));
            var authState = new AuthenticationState(user);
            _authStateProvider
                .Setup(p => p.GetAuthenticationStateAsync())
                .ReturnsAsync(authState);
        }

        public void SetNotAuthorized()
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity());
            var authState = new AuthenticationState(user);
            _authStateProvider
                .Setup(p => p.GetAuthenticationStateAsync())
                .ReturnsAsync(authState);
        }

        public void SetAuthorizedWithRole(string username, string role)
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role)
            }, "TestAuthType"));
            var authState = new AuthenticationState(user);
            _authStateProvider
                .Setup(p => p.GetAuthenticationStateAsync())
                .ReturnsAsync(authState);
        }
    }
}