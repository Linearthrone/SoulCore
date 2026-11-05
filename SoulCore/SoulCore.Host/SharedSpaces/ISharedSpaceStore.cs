using SoulCore.Core.SharedSpaces;

namespace SoulCore.Host.SharedSpaces;

public interface ISharedSpaceStore
{
    Task<IReadOnlyList<SharedSpace>> ListAsync(CancellationToken ct = default);

    Task<SharedSpace?> GetAsync(string spaceId, CancellationToken ct = default);

    Task<SharedSpace> UpsertAsync(SharedSpace space, CancellationToken ct = default);

    Task<bool> DeleteAsync(string spaceId, CancellationToken ct = default);

    SharedSpaceMemory OpenMemory(string spaceId);
}
