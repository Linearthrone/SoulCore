using Microsoft.Extensions.DependencyInjection;
using SoulCore.Host.SharedSpaces;

namespace SoulCore.Host.Hosting.ServiceCollectionExtensions;

internal static class SharedSpaceServiceCollectionExtensions
{
    internal static IServiceCollection AddSharedSpaces(this IServiceCollection services)
    {
        services.AddSingleton<ISharedSpaceStore, SharedSpaceStore>();
        services.AddSingleton<ISharedSpaceContextBuilder, SharedSpaceContextBuilder>();
        return services;
    }
}
