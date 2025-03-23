using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using BlazorApp3.Data;

namespace BlazorApp3.Endpoints
{
    public static class CompleteRegistrationEndpoint
    {
        public static void MapCompleteRegistrationEndpoint(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet("/Account/CompleteRegistration", async (
                string userId,
                string returnUrl,
                [FromServices] SignInManager<ApplicationUser> signInManager,
                [FromServices] UserManager<ApplicationUser> userManager) =>
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return Results.BadRequest("Invalid user ID");
                }

                var user = await userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Results.BadRequest("User not found");
                }

                // Sign in the user
                await signInManager.SignInAsync(user, isPersistent: false);

                // Redirect to the return URL or a default location
                returnUrl = !string.IsNullOrEmpty(returnUrl) ? returnUrl : "/";

                return Results.Redirect(returnUrl);
            });
        }
    }
}