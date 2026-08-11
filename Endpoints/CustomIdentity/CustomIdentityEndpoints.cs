using System.Text;
using AeonRegistryAPI.Endpoints.CustomIdentity.Models;
using AeonRegistryAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
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
            .WithDescription("Registers a user; must have admin role")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPost("/reset-password", ResetPassword)
            .WithName("ResetPassword")
            .WithSummary("Resets a user's password")
            .WithDescription("Resets a user's password")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);
        
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

    private static async Task<IResult> ResetPassword(ResetPasswordRequest request,
        UserManager<ApplicationUser> userManager)
    {
        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.ResetCode) ||
            string.IsNullOrEmpty(request.NewPassword))
        {
            return Results.BadRequest(new { Message = $"Please fill all the required fields: Email, ResetCode, NewPassword" });
        }
        
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return Results.BadRequest(new { Message = $"User with email {request.Email} does not exist" });
        }

        try
        {
            var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.ResetCode));
            var resetResult = await userManager.ResetPasswordAsync(user, decodedToken, request.NewPassword);

            if (!resetResult.Succeeded)
            {
                return Results.BadRequest(resetResult.Errors.Select(e => e.Description));
            }
        }
        catch (FormatException)
        {
            return Results.BadRequest(new { Message = $"Invalid reset code entered." });
        }
        catch (Exception e)
        {
            return Results.BadRequest(new { Message = $"Error: {e.Message}" });
        }
        
        return Results.Ok(new { Message = "Successfully reset password" });
    }
}