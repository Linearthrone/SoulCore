using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SoulCore.Host.Persona;

/// <summary>Seeds pack store and loads the active persona before chat traffic.</summary>
public sealed class PersonaSessionHostedService : IHostedService
{
    private readonly ActivePersonaSession _session;
    private readonly ILogger<PersonaSessionHostedService> _logger;

    public PersonaSessionHostedService(
        ActivePersonaSession session,
        ILogger<PersonaSessionHostedService> logger)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _session.InitializeAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Fall back to in-memory Blank so Host still boots without Victoria.
            _logger.LogError(ex, "Persona session init failed; continuing with in-memory Blank pack");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
