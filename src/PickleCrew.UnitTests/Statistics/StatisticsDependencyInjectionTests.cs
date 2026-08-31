using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PickleCrew.Api;
using PickleCrew.Api.Statistics.Services;
using Xunit;

namespace PickleCrew.UnitTests.Statistics;

public sealed class StatisticsDependencyInjectionTests
{
    [Fact]
    public void AddStatisticsServices_WhenRebuildIsEnabled_ShouldRegisterRebuildHostedService()
    {
        var configuration = CreateConfiguration(rebuildStatistics: true);
        var services = new ServiceCollection();

        services.AddStatisticsServices(configuration);

        Assert.Contains(
            services,
            descriptor =>
                descriptor.ServiceType == typeof(IHostedService) &&
                descriptor.ImplementationType == typeof(StatisticsRebuildHostedService)
        );
    }

    [Fact]
    public void AddStatisticsServices_WhenRebuildIsDisabled_ShouldNotRegisterRebuildHostedService()
    {
        var configuration = CreateConfiguration(rebuildStatistics: false);
        var services = new ServiceCollection();

        services.AddStatisticsServices(configuration);

        Assert.DoesNotContain(
            services,
            descriptor => descriptor.ImplementationType == typeof(StatisticsRebuildHostedService)
        );
    }

    private static IConfiguration CreateConfiguration(bool rebuildStatistics)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RebuildStatistics"] = rebuildStatistics.ToString(),
            })
            .Build();
    }
}
