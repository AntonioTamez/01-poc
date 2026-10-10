using System.Reflection;
using NetArchTest.Rules;
using Orders.Domain.Orders;
using ApplicationLayer = Orders.Application.DependencyInjection;
using InfrastructureLayer = Orders.Infrastructure.DependencyInjection;

namespace Orders.ArchitectureTests;

/// <summary>Enforces the dependency rule: Api -> Infrastructure -> Application -> Domain.</summary>
public sealed class LayerDependencyTests
{
    private const string DomainNamespace = "Orders.Domain";
    private const string ApplicationNamespace = "Orders.Application";
    private const string InfrastructureNamespace = "Orders.Infrastructure";
    private const string ApiNamespace = "Orders.Api";

    private static readonly Assembly DomainAssembly = typeof(Order).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(ApplicationLayer).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(InfrastructureLayer).Assembly;

    [Fact]
    public void Domain_DoesNotDependOnOuterLayersOrFrameworks()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                ApplicationNamespace,
                InfrastructureNamespace,
                ApiNamespace,
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore",
                "Microsoft.Extensions")
            .GetResult();

        AssertSuccessful(result);
    }

    [Fact]
    public void Domain_ReferencesOnlyFrameworkAndBuildingBlocksAssemblies()
    {
        var disallowed = DomainAssembly.GetReferencedAssemblies()
            .Select(reference => reference.Name!)
            .Where(name => !name.StartsWith("System", StringComparison.Ordinal)
                && name != "netstandard"
                && name != "BuildingBlocks.Abstractions")
            .ToList();

        Assert.Empty(disallowed);
    }

    [Fact]
    public void Application_DoesNotDependOnInfrastructureOrApi()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(InfrastructureNamespace, ApiNamespace, "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore")
            .GetResult();

        AssertSuccessful(result);
    }

    [Fact]
    public void Infrastructure_DoesNotDependOnApi()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        AssertSuccessful(result);
    }

    [Theory]
    [InlineData(DomainNamespace)]
    [InlineData(ApplicationNamespace)]
    [InlineData(InfrastructureNamespace)]
    public void OrdersAssemblies_DoNotReferenceOtherServices(string layerNamespace)
    {
        var assembly = layerNamespace switch
        {
            DomainNamespace => DomainAssembly,
            ApplicationNamespace => ApplicationAssembly,
            _ => InfrastructureAssembly,
        };

        var foreignServices = assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name!)
            .Where(name => !name.StartsWith("Orders.", StringComparison.Ordinal)
                && (name.EndsWith(".Domain", StringComparison.Ordinal)
                    || name.EndsWith(".Application", StringComparison.Ordinal)
                    || name.EndsWith(".Infrastructure", StringComparison.Ordinal)
                    || name.EndsWith(".Api", StringComparison.Ordinal)))
            .ToList();

        Assert.Empty(foreignServices);
    }

    private static void AssertSuccessful(NetArchTest.Rules.TestResult result)
    {
        var offenders = result.FailingTypeNames ?? [];
        Assert.True(result.IsSuccessful, $"Dependency rule violated by: {string.Join(", ", offenders)}");
    }
}
