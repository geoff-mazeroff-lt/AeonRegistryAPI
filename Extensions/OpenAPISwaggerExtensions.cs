using AeonRegistryAPI.Filters;
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
                
                [Launch Public Test Site](/site/sites-map.html)
                
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

            // We have to hide the out-of-the-box endpoints because we're customizing how we work with ASP.NET Identity.
            // CustomIdentityEndpoints defines other endpoints with slightly different names (e.g., reset-password
            // instead of resetpassword) so we can still provide that functionality.
            // Note there is no `/` prefix.
            string[] endPointsToHide = [
                "api/auth/register",
                "api/auth/refresh",
                "api/auth/confirmemail",
                "api/auth/resendconfirmationemail",
                "api/auth/forgotpassword",
                "api/auth/resetpassword",
                "api/auth/manage",
                "api/auth/manage/info",
                "api/auth/manage/2fa"
            ];
            
            c.DocInclusionPredicate((docName, description) =>
            {
                var path = description.RelativePath?.ToLowerInvariant();
                if (path is null)
                    return false;
                
                return !endPointsToHide.Contains(path, StringComparer.OrdinalIgnoreCase);
            });
            
            c.SchemaFilter<EnumStringSchemaFilter>();
        });
        
        return services;
    }
}