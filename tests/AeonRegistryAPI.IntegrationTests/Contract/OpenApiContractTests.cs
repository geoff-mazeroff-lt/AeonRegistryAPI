using AeonRegistryAPI.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.Swagger;

namespace AeonRegistryAPI.IntegrationTests.Contract;

/// <summary>
/// Level 4: diffs the generated OpenAPI document against a committed snapshot.
/// </summary>
/// <remarks>
/// This does not assert the API is correct - only that its published surface has not changed
/// without someone noticing. A failure here means "you changed the contract," which is a prompt
/// to think about consumers, not necessarily a bug. Accept an intentional change by deleting the
/// generated <c>*.received.json</c> after reviewing it and running the test again to regenerate
/// <c>*.verified.json</c>, then committing the result.
/// </remarks>
public class OpenApiContractTests(AeonApiFactory factory) : IClassFixture<AeonApiFactory>
{
    [Fact]
    public async Task GetSwaggerDocument_MatchesTheCommittedSnapshot()
    {
        // ISwaggerProvider is resolvable here even though UseSwagger()/UseSwaggerUI() never run
        // in the "Testing" environment: AddCustomSwagger registers the provider unconditionally,
        // and only the middleware that exposes it over HTTP is Development-gated.
        var swagger = factory.Services.GetRequiredService<ISwaggerProvider>().GetSwagger("v1");

        // Microsoft.OpenApi 2.x dropped the SerializeAsJson convenience extension the plan
        // originally named; SerializeAs against an OpenApiJsonWriter is the current equivalent.
        using var stringWriter = new StringWriter();
        swagger.SerializeAs(OpenApiSpecVersion.OpenApi3_0, new OpenApiJsonWriter(stringWriter));

        await Verify(stringWriter.ToString(), extension: "json");
    }
}
