namespace AeonRegistryAPI.Filters;

public class ExceptionHandlingFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (Exception ex)
        {
            var environment = context.HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
            Console.WriteLine($"Exception: {ex.Message}");
            return Results.Problem(
                detail: environment.IsDevelopment() ? ex.ToString() : null,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "An unexpected error occured"); 
        }
    }
}