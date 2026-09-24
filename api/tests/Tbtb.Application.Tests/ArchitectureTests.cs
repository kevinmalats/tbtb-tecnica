using Tbtb.Application.UseCases;
using Xunit;

namespace Tbtb.Application.Tests;

public sealed class ArchitectureTests
{
    [Fact]
    public void DomainAndApplicationDoNotReferenceInfrastructureFrameworks()
    {
        var forbidden = new[] { "Microsoft.EntityFrameworkCore", "Tbtb.Infrastructure", "Tbtb.Api" };
        var domainReferences = typeof(Tbtb.Domain.Patient).Assembly.GetReferencedAssemblies().Select(x => x.Name);
        var applicationReferences = typeof(ITbtbUseCases).Assembly.GetReferencedAssemblies().Select(x => x.Name);

        Assert.DoesNotContain(domainReferences, forbidden.Contains);
        Assert.DoesNotContain(applicationReferences, forbidden.Contains);
    }

    [Fact]
    public void UseCaseResultCarriesContractErrorWithoutHttpDependency()
    {
        var result = UseCaseResult.Invalid(new() { ["month"] = ["Mes invalido."] });

        Assert.Equal(400, result.Status);
        Assert.Equal("VALIDATION_ERROR", result.Code);
        Assert.Equal("Mes invalido.", result.Errors!["month"].Single());
    }
}
