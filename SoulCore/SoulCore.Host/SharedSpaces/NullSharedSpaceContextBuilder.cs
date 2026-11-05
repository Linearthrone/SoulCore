using SoulCore.Core.Persona;

namespace SoulCore.Host.SharedSpaces;

public sealed class NullSharedSpaceContextBuilder : ISharedSpaceContextBuilder
{
    public Task<string> BuildAttachedContextAsync(
        PersonaPack pack,
        int perSpaceLimit = 8,
        CancellationToken ct = default) =>
        Task.FromResult(string.Empty);
}
