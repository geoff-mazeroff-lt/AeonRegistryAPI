using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;

namespace AeonRegistryAPI.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ArtifactType
{
    Weapon = 1,
    EnergySource = 2,
    CommunicationDevice = 3,
    Machine = 4,
    Tool = 5,
    Monolith = 6,
    Device = 7,
    Unknown = 99
}