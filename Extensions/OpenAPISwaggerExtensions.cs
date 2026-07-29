using Microsoft.OpenApi;

namespace AeonRegistryAPI.Extensions;

public static class OpenApiSwaggerExtensions
{
    public static IServiceCollection AddCustomSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Aeon Registry API",
                Version = "v1",
                Description = """
                
                <img src="/images/AeonRegistryLogoBLK.png" height="120" />
                ## Aeon Research Division
                
                Internal API for managing recovered artifacts and research data.
                Provides secure access for field researchers and analysts.
                
                ### Key Features
                - Site and artifact catalog
                - Research record submissions
                - Secure media storage
                - User role management
                
                """,
                Contact = new OpenApiContact
                {
                    Name = "Aeon Registry Team",
                    Url = new Uri("https://github.com/geoff-mazeroff-lt/AeonRegistryAPI"),
                }
            });
             c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                 {
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter 'Bearer' [space] and then your token in the text input below."
                 }
             );
            c.AddSecurityRequirement(document => new() { [new OpenApiSecuritySchemeReference("Bearer", document)] = []});
        });
        
        return services;
    }
}