using Microsoft.AspNetCore.Http.HttpResults;

namespace AeonRegistryAPI.Endpoints.Home;

public static class HomeEndpoints
{
    public static IEndpointRouteBuilder MapHomeEndpoints(this IEndpointRouteBuilder route)
    {
        // A group has one or more endpoints.
        var homeGroup = route.MapGroup("/api/home")
            .WithTags("Home");
        
        // The route is appended to the group -- /api/home/welcome.
        homeGroup.MapGet("/welcome", GetWelcomeMessage)
            .WithName("GetWelcomeMessage")
            .WithSummary("Welcome Message")
            .WithDescription("Displays a welcome message");
            
        return route;
    }
    
    // Handler
    private static async Task<Ok<WelcomeResponse>> GetWelcomeMessage(CancellationToken ct)
    {
        // Note: Nothing to await here. Normally there would be a service instance passed in
        // that would have an awaitable method.
        var response = new WelcomeResponse
        {
            Message = "Welcome to the Aeon Registry API!",
            Version = "1.0.0",
            TimeOnly = DateTime.Now.ToString("T")
        };
        return TypedResults.Ok(response);
    }
}