using System.Text.Json.Serialization;

namespace AeonRegistryAPI.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CatalogStatus
{
    Draft = 1,
    Verified = 2,
    Archived = 3,
}