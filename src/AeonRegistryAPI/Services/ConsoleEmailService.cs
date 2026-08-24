using Microsoft.AspNetCore.Identity.UI.Services;

namespace AeonRegistryAPI.Services;

public class ConsoleEmailService : IEmailSender
{
    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        Console.WriteLine($"To:{email}\nSubject:{subject}\n\nMessage:{htmlMessage}");
        return Task.CompletedTask;
    }
}