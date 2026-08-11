using System.Text;
using AeonRegistryAPI.Endpoints.CustomIdentity.Models;
using AeonRegistryAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.WebUtilities;

namespace AeonRegistryAPI.Endpoints.CustomIdentity;

public static class CustomIdentityEndpoints
{
    public static IEndpointRouteBuilder MapCustomIdentityEndpoints(this IEndpointRouteBuilder route)
    {
        var group = route.MapGroup("/api/auth")
            .WithTags("Admin");

        group.MapPost("/register-admin", RegisterUser)
            .WithName("RegisterAdmin")
            .WithSummary("Register a User")
            .WithDescription("Registers a user; must have admin role");
        
        return route;
    }

    private static async Task<IResult> RegisterUser(RegisterUserRequest request, 
        UserManager<ApplicationUser> userManager, 
        RoleManager<IdentityRole> roleManager, 
        IEmailSender emailSender,
        IConfiguration configuration)
    {
        if (await userManager.FindByEmailAsync(request.Email) is not null)
        {
            return Results.BadRequest($"User with email {request.Email} already exists");
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
        };
        
        // Note: This must meet the password requirements.
        // In a real system, you'd generate this.
        var tempPassword = "TempPassword123!"; 
        var result = await userManager.CreateAsync(user, tempPassword);
        var passwordResetToken = await userManager.GeneratePasswordResetTokenAsync(user);
        var encodedResetToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(passwordResetToken));

        if (!result.Succeeded)
        {
            return Results.BadRequest(result.Errors.Select(e => e.Description));
        }

        if (await roleManager.RoleExistsAsync("Researcher"))
        {
            await userManager.AddToRoleAsync(user, "Researcher");
        }
        
        // Send email to change password
        var baseUrl = configuration["BaseUrl"] ?? "https://localhost:7132";
        await emailSender.SendEmailAsync(request.Email, "Welcome to Aeon Registry!",
            $"""
             Your account has been created. Please change your password by visiting: 
             {baseUrl}/SetPassword.html?email={request.Email}&resetCode={encodedResetToken}
             """);
        
        return Results.Ok(new { Message = $"Successfully registered user {user.Email}. Password reset link sent." });
    }
}