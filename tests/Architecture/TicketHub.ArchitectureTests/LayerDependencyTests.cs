using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace TicketHub.ArchitectureTests;

/// <summary>
/// Enforces Clean Architecture boundaries automatically (not "on the honour system"). These
/// tests fail the build if a layer takes a forbidden dependency.
/// </summary>
public class LayerDependencyTests
{
    private static readonly Assembly BuildingBlocksDomain = typeof(BuildingBlocks.Domain.Results.Result).Assembly;
    private static readonly Assembly CatalogDomain = typeof(Catalog.Domain.CatalogErrors).Assembly;
    private static readonly Assembly CatalogApplication = typeof(Catalog.Application.DependencyInjection).Assembly;
    private static readonly Assembly BookingDomain = typeof(Booking.Domain.BookingErrors).Assembly;
    private static readonly Assembly BookingApplication = typeof(Booking.Application.DependencyInjection).Assembly;
    private static readonly Assembly PaymentDomain = typeof(Payment.Domain.PaymentRecord).Assembly;
    private static readonly Assembly NotificationsDomain = typeof(Notifications.Domain.Notification).Assembly;

    private static readonly string[] InfrastructureDependencies =
    {
        "Microsoft.EntityFrameworkCore",
        "Npgsql",
        "MassTransit",
        "StackExchange.Redis",
        "MongoDB",
        "Microsoft.AspNetCore",
        "Grpc"
    };

    public static IEnumerable<object[]> DomainAssemblies()
    {
        yield return new object[] { BuildingBlocksDomain };
        yield return new object[] { CatalogDomain };
        yield return new object[] { BookingDomain };
        yield return new object[] { PaymentDomain };
        yield return new object[] { NotificationsDomain };
    }

    [Theory]
    [MemberData(nameof(DomainAssemblies))]
    public void Domain_should_not_depend_on_infrastructure_or_frameworks(Assembly domain)
    {
        TestResult result = Types.InAssembly(domain)
            .ShouldNot()
            .HaveDependencyOnAny(InfrastructureDependencies)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Domain must stay free of infrastructure/framework dependencies. Offenders: {0}",
            string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>()));
    }

    [Theory]
    [InlineData(typeof(Catalog.Application.DependencyInjection), typeof(Catalog.Domain.CatalogErrors))]
    [InlineData(typeof(Booking.Application.DependencyInjection), typeof(Booking.Domain.BookingErrors))]
    public void Application_should_not_depend_on_infrastructure(Type applicationMarker, Type _)
    {
        TestResult result = Types.InAssembly(applicationMarker.Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore", "Npgsql", "MassTransit",
                "StackExchange.Redis", "MongoDB", "Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            "Application must not depend on Infrastructure concerns. Offenders: {0}",
            string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>()));
    }

    [Fact]
    public void Catalog_domain_should_not_depend_on_application()
    {
        TestResult result = Types.InAssembly(CatalogDomain)
            .ShouldNot()
            .HaveDependencyOn("Catalog.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Booking_domain_should_not_depend_on_application()
    {
        TestResult result = Types.InAssembly(BookingDomain)
            .ShouldNot()
            .HaveDependencyOn("Booking.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Domain_does_not_reference_other_services_domains()
    {
        // Sharing domain models between services is forbidden (hidden coupling).
        TestResult result = Types.InAssembly(CatalogDomain)
            .ShouldNot()
            .HaveDependencyOnAny("Booking.Domain", "Payment.Domain", "Notifications.Domain")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }
}
