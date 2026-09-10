using AeonRegistryAPI.Models;
using AeonRegistryAPI.Models.Request;

namespace AeonRegistryAPI.IntegrationTests.TestData;

/// <summary>
/// Builds a valid <see cref="Site"/> (or the matching create/update request) with sensible
/// defaults, so a test only has to state the values it actually cares about.
/// </summary>
/// <remarks>
/// <para>
/// The point is readability, not saved keystrokes. <c>new SiteBuilder().WithName(null)</c>
/// says "a site that is normal except its name is missing" - the one interesting input
/// stands out against a background of defaults. Constructing the entity inline instead
/// buries that one line among seven irrelevant ones, and the next reader cannot tell which
/// of the eight the test is about.
/// </para>
/// <para>
/// Hand-rolled and fully deterministic rather than Bogus/AutoFixture: no extra dependency,
/// and every test run gets the same values, so a failure is reproducible. Bogus is a
/// reasonable addition when you need <i>bulk</i> data (a hundred sites for a paging test)
/// where the specific values genuinely do not matter - but random data in an assertion is
/// how tests become intermittently red.
/// </para>
/// <para>
/// Each <c>With...</c> returns <c>this</c>, so the builder is mutable and chainable. That
/// is fine for a per-test throwaway; do not share one instance between tests.
/// </para>
/// </remarks>
public sealed class SiteBuilder
{
    private string? _name = "Ashfall Terrace";
    private string? _location = "Northern Reach, Sector 12";
    private string? _coordinates = "64.1466° N, 21.9426° W";
    private double _latitude = 64.1466;
    private double _longitude = -21.9426;
    private string? _description = "A terraced excavation cut into volcanic ash.";
    private string? _publicNarrative = "Visitors may view the lower terrace from the walkway.";
    private string? _aeonNarrative = "Restricted: resonance readings remain unexplained.";

    public SiteBuilder WithName(string? name)
    {
        _name = name;
        return this;
    }

    public SiteBuilder WithLocation(string? location)
    {
        _location = location;
        return this;
    }

    /// <summary>Clears <see cref="Site.Location"/>, which is <c>[Required]</c>.</summary>
    /// <remarks>
    /// Named for the <i>scenario</i> rather than the mechanics: a test reading
    /// <c>.WithoutLocation()</c> is obviously about a missing required field, where
    /// <c>.WithLocation(null)</c> only says a null went in somewhere.
    /// </remarks>
    public SiteBuilder WithoutLocation() => WithLocation(null);

    public SiteBuilder WithCoordinates(string? coordinates)
    {
        _coordinates = coordinates;
        return this;
    }

    public SiteBuilder WithPosition(double latitude, double longitude)
    {
        _latitude = latitude;
        _longitude = longitude;
        return this;
    }

    public SiteBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    public SiteBuilder WithPublicNarrative(string? publicNarrative)
    {
        _publicNarrative = publicNarrative;
        return this;
    }

    /// <summary>
    /// Sets the Aeon (staff-only) narrative - the field the public projections must never
    /// expose.
    /// </summary>
    public SiteBuilder WithAeonNarrative(string? aeonNarrative)
    {
        _aeonNarrative = aeonNarrative;
        return this;
    }

    public Site Build() => new()
    {
        Name = _name,
        Location = _location,
        Coordinates = _coordinates,
        Latitude = _latitude,
        Longitude = _longitude,
        Description = _description,
        PublicNarrative = _publicNarrative,
        AeonNarrative = _aeonNarrative
    };

    /// <remarks>
    /// The entity and the two request records have the same eight fields today, so one
    /// builder can produce all three. If they ever diverge - and DTOs usually should be free
    /// to - split this into separate builders rather than adding flags here.
    /// </remarks>
    public CreateSiteRequest BuildCreateRequest() => new()
    {
        Name = _name,
        Location = _location,
        Coordinates = _coordinates,
        Latitude = _latitude,
        Longitude = _longitude,
        Description = _description,
        PublicNarrative = _publicNarrative,
        AeonNarrative = _aeonNarrative
    };

    public UpdateSiteRequest BuildUpdateRequest() => new()
    {
        Name = _name,
        Location = _location,
        Coordinates = _coordinates,
        Latitude = _latitude,
        Longitude = _longitude,
        Description = _description,
        PublicNarrative = _publicNarrative,
        AeonNarrative = _aeonNarrative
    };
}
