namespace AeonRegistryAPI.Middleware;

public class BlockIdentityEndpoints(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    // Note the `/` prefix
    private static readonly string[] BlockedPaths = [
        "/api/auth/register",
        "/api/auth/forgotpassword",
        "/api/auth/resetpassword",
        "/api/auth/manage/info",
        "/api/auth/manage/profile"
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant();
        if (path is not null && BlockedPaths.Contains(path))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(new
                {
                    Message = $"Endpoint '{path}' is disabled.",
                }
            );
            
            // Stop the request pipeline
            return;
        }
        
        // Continue the request pipeline
        await _next(context);
    }
}