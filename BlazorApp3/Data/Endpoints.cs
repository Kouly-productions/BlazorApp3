// Create this as a new file: CompleteSignInEndpoint.cs
using BlazorApp3.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BlazorApp3.Endpoints
{
    public static class CompleteSignInEndpoint
    {
        public static void MapCompleteSignInEndpoint(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet("/Account/CompleteSignIn", async (
                [FromQuery] string userId,
                [FromQuery] bool rememberMe,
                [FromQuery] string returnUrl,
                SignInManager<ApplicationUser> signInManager,
                UserManager<ApplicationUser> userManager) =>
            {
                var user = await userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Results.Redirect("/Account/Login");
                }

                await signInManager.SignInAsync(user, rememberMe);
                return Results.Redirect(returnUrl ?? "/");
            });
        }
    }
}