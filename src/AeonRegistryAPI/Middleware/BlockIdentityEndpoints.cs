namespace AeonRegistryAPI.Middleware;

public class BlockIdentityEndpoints(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    // Note the `/` prefix, and no trailing slash: InvokeAsync normalizes the incoming path to
    // this shape before comparing.
    // Every built-in Identity endpoint this API does not mean to expose. Keep this in step with
    // `endPointsToHide` in OpenApiSwaggerExtensions: hiding a route from Swagger without
    // blocking it leaves it live and merely undocumented, which is obscurity, not security.
    private static readonly string[] BlockedPaths = [
        "/api/auth/register",
        "/api/auth/refresh",
        "/api/auth/confirmemail",
        "/api/auth/resendconfirmationemail",
        "/api/auth/forgotpassword",
        "/api/auth/resetpassword",
        "/api/auth/manage/info",
        "/api/auth/manage/2fa",
        "/api/auth/manage/profile"
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        // Normalize the way the router does before comparing. ASP.NET Core matches URL paths
        // case-insensitively and ignores a trailing slash, so "/API/Auth/Register/" reaches the
        // same Identity endpoint as "/api/auth/register". A block list that compares the raw
        // path therefore has bypasses in it - one of them a single extra character.
        var path = context.Request.Path.Value?.ToLowerInvariant().TrimEnd('/');
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