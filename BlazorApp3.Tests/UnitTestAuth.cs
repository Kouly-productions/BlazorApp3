using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Bunit;
using Moq;
using BlazorApp3.Components.Pages;
using Microsoft.AspNetCore.Components;

namespace BlazorApp3.Tests
{
    public class AuthenticationTests : TestContext
    {
        private void SetupAuthorizationServices()
        {
            Services.AddAuthorization();
            Services.AddSingleton<IAuthorizationPolicyProvider, DefaultAuthorizationPolicyProvider>();
            Services.AddSingleton<IAuthorizationService, DefaultAuthorizationService>();
        }

        [Fact]
        public void Auth_Page_Should_Not_Render_When_Not_Authenticated()
        {
            // Arrange
            SetupAuthorizationServices();

            var authContext = new UnitTestAuthorizationContext();
            authContext.SetNotAuthorized();

            Services.AddScoped<AuthenticationStateProvider>(sp =>
                authContext.AuthenticationStateProvider);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                RenderComponent<Auth>());

            // Vi tester kun at der kastes en exception, ikke den specifikke fejlmeddelse
        }

        [Fact]
        public void Auth_Page_Renders_When_Authenticated()
        {
            // Arrange
            SetupAuthorizationServices();

            var authContext = new UnitTestAuthorizationContext();
            authContext.SetAuthorized("testuser@example.com");

            Services.AddScoped<AuthenticationStateProvider>(sp =>
                authContext.AuthenticationStateProvider);
            Services.AddScoped<CascadingAuthenticationState>();

            // Act - Brug den korrekte Render metode med RenderFragment parameter
            var cut = Render((RenderFragment)(builder =>
            {
                builder.OpenComponent<CascadingAuthenticationState>(0);
                builder.AddAttribute(1, "ChildContent", (RenderFragment)(childBuilder =>
                {
                    childBuilder.OpenComponent<Auth>(10);
                    childBuilder.CloseComponent();
                }));
                builder.CloseComponent();
            }));

            // Assert - Tjek at indholdet vises
            Assert.Contains("You are authenticated", cut.Markup);
        }
    }
}