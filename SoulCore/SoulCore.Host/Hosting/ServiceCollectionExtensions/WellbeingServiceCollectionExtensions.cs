using Microsoft.Extensions.DependencyInjection;
using SoulCore.Host.Wellbeing;

namespace SoulCore.Host.Hosting.ServiceCollectionExtensions;

internal static class WellbeingServiceCollectionExtensions
{
    internal static IServiceCollection AddWellbeingRuntime(this IServiceCollection services)
    {
        services.AddSingleton<SettlingState>();
        services.AddSingleton<IObservatoryStore, ObservatoryStore>();
        services.AddSingleton<ISoftSwitchService, SoftSwitchService>();
        return services;
    }
}
