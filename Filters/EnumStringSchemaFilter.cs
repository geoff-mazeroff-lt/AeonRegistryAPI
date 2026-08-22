using AeonRegistryAPI.Enums;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AeonRegistryAPI.Filters;

public class EnumStringSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(string) && context.MemberInfo?.Name == "Type")
        {
            
            // "Type" is a generic sounding property name. Ensure we're only applying enum
            // names for the appropriate type.
            var declaringTypeName = context.MemberInfo?.DeclaringType?.Name;
            if (declaringTypeName != null && declaringTypeName.EndsWith("ArtifactRequest"))
            {
                schema.Description = "Allowed values: " + string.Join(", ", Enum.GetNames<ArtifactType>());
            }
        }
    }
}