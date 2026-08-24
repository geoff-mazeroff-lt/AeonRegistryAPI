namespace AeonRegistryAPI.Data;

public static class DataUtility
{
    public static string? GetConnectionString(IConfiguration configuration)
    {
        return configuration.GetConnectionString("DbConnection");
    }
}