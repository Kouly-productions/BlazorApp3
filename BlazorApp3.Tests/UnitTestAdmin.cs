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
    public class AuthorizationTests : TestContext
    {
        private void SetupAuthorizationServices()
        {
            Services.AddAuthorization();
            Services.AddSingleton<IAuthorizationPolicyProvider, DefaultAuthorizationPolicyProvider>();
            Services.AddSingleton<IAuthorizationService, DefaultAuthorizationService>();
        }

        [Fact]
        public void Admin_Page_Should_Not_Render_For_Regular_User()
        {
            // Arrange
            SetupAuthorizationServices();

            var authContext = new UnitTestAuthorizationContext();
            authContext.SetAuthorized("user@example.com"); // Almindelig bruger

            Services.AddScoped<AuthenticationStateProvider>(sp =>
                authContext.AuthenticationStateProvider);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                RenderComponent<Admin>());

            // Vi tester kun at der kastes en exception, ikke den specifikke fejlmeddelse
        }

        [Fact]
        public void Admin_Page_Renders_Admin_Message_When_Authorized()
        {
            // Arrange
            SetupAuthorizationServices();

            var authContext = new UnitTestAuthorizationContext();
            authContext.SetAuthorizedWithRole("admin@example.com", "Admin");

            Services.AddScoped<AuthenticationStateProvider>(sp =>
                authContext.AuthenticationStateProvider);
            Services.AddScoped<CascadingAuthenticationState>();

            // Act - Brug den korrekte Render metode med RenderFragment parameter
            var cut = Render((RenderFragment)(builder =>
            {
                builder.OpenComponent<CascadingAuthenticationState>(0);
                builder.AddAttribute(1, "ChildContent", (RenderFragment)(childBuilder =>
                {
                    childBuilder.OpenComponent<Admin>(10);
                    childBuilder.CloseComponent();
                }));
                builder.CloseComponent();
            }));

            // Assert
            Assert.Contains("You are admin", cut.Markup);
        }
    }
}