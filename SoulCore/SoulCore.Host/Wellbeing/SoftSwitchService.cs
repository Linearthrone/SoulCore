using System.Text.Json;
using Microsoft.Extensions.Logging;
using SoulCore.Core.Persona;
using SoulCore.Core.Wellbeing;
using SoulCore.Host.Persona;

namespace SoulCore.Host.Wellbeing;

public sealed class SoftSwitchService : ISoftSwitchService
{
    private readonly IPersonaSession _session;
    private readonly IPersonaPackStore _packs;
    private readonly IPersonaStoreHub _stores;
    private readonly SettlingState _settling;
    private readonly IObservatoryStore _observatory;
    private readonly ILogger<SoftSwitchService> _logger;

    public SoftSwitchService(
        IPersonaSession session,
        IPersonaPackStore packs,
        IPersonaStoreHub stores,
        SettlingState settling,
        IObservatoryStore observatory,
        ILogger<SoftSwitchService> logger)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _packs = packs ?? throw new ArgumentNullException(nameof(packs));
        _stores = stores ?? throw new ArgumentNullException(nameof(stores));
        _settling = settling ?? throw new ArgumentNullException(nameof(settling));
        _observatory = observatory ?? throw new ArgumentNullException(nameof(observatory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public SettlingSnapshot? GetSettlingSnapshot() => _settling.TryGetSnapshot();

    public async Task<SoftSwitchResult> SoftActivateAsync(
        string personaId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(personaId);
        var targetId = personaId.Trim();

        if (_settling.BlocksActivate(targetId, out var blockDetail))
        {
            var blocked = await _observatory.RecordStimulusAsync(
                    StimulusKinds.PersonaSwitchBlockedSettling,
                    _session.ActivePersonaId,
                    targetId,
                    JsonSerializer.Serialize(new { reason = blockDetail }),
                    cancellationToken)
                .ConfigureAwait(false);

            return new SoftSwitchResult(
                Ok: false,
                Persona: null,
                StimulusId: blocked.StimulusId,
                Note: blockDetail!,
                SettlingTurnsRemaining: _settling.TryGetSnapshot()?.RemainingTurns,
                StatusCode: 409);
        }

        var outgoing = _session.GetActive();
        if (string.Equals(outgoing.PersonaId, targetId, StringComparison.OrdinalIgnoreCase))
        {
            return new SoftSwitchResult(
                Ok: true,
                Persona: outgoing,
                StimulusId: null,
                Note: "Already active; no soft switch required.",
                SettlingTurnsRemaining: _settling.TryGetSnapshot()?.RemainingTurns);
        }

        var incomingPack = await _packs.GetAsync(targetId, cancellationToken).ConfigureAwait(false);
        if (incomingPack is null)
        {
            return new SoftSwitchResult(
                Ok: false,
                Persona: null,
                StimulusId: null,
                Note: $"Unknown personaId '{targetId}'.",
                SettlingTurnsRemaining: null,
                StatusCode: 404);
        }

        var incomingDisplay = string.IsNullOrWhiteSpace(incomingPack.DisplayName)
            ? targetId
            : incomingPack.DisplayName;

        // 1) Closing narrative journal while outgoing stores are still active.
        var closeBody = NarrativeMentalState.BuildOutgoingSwitchClose(
            outgoing.DisplayName,
            incomingDisplay,
            targetId);
        try
        {
            var journals = _stores.GetJournalStore(outgoing.PersonaId);
            await journals.WriteEntryAsync(
                    NarrativeMentalState.FeelingBookId,
                    closeBody,
                    tagsJson: """["soft_switch","outgoing_close"]""",
                    source: "system",
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Outgoing soft-switch journal failed for {PersonaId}", outgoing.PersonaId);
        }

        var stimulus = await _observatory.RecordStimulusAsync(
                StimulusKinds.PersonaSoftSwitch,
                targetId,
                outgoing.PersonaId,
                JsonSerializer.Serialize(new
                {
                    fromPersonaId = outgoing.PersonaId,
                    fromDisplayName = outgoing.DisplayName,
                    toPersonaId = targetId,
                    mode = "soft"
                }),
                cancellationToken)
            .ConfigureAwait(false);

        try
        {
            await _observatory.AttachResponseAsync(
                    stimulus.StimulusId,
                    "journal.outgoing_close",
                    outgoing.PersonaId,
                    closeBody,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Observatory attach outgoing journal failed");
        }

        // 2) Low-level activate (store reopen) — only after close journal.
        await _session.SetActiveAsync(targetId, cancellationToken).ConfigureAwait(false);
        var incoming = _session.GetActive();

        await _observatory.RecordStimulusAsync(
                StimulusKinds.MemoryStoreReopen,
                incoming.PersonaId,
                outgoing.PersonaId,
                JsonSerializer.Serialize(new { afterSoftSwitch = true, stimulusId = stimulus.StimulusId }),
                cancellationToken)
            .ConfigureAwait(false);

        // 3) Arrival journal on incoming brain.
        var arrivalBody = NarrativeMentalState.BuildIncomingSwitchArrival(
            incoming.DisplayName,
            outgoing.DisplayName,
            outgoing.PersonaId,
            stimulus.StimulusId);
        try
        {
            var journals = _stores.GetJournalStore(incoming.PersonaId);
            await journals.WriteEntryAsync(
                    NarrativeMentalState.FeelingBookId,
                    arrivalBody,
                    tagsJson: """["soft_switch","incoming_arrival"]""",
                    source: "system",
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            await _observatory.AttachResponseAsync(
                    stimulus.StimulusId,
                    "journal.incoming_arrival",
                    incoming.PersonaId,
                    arrivalBody,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Incoming soft-switch journal failed for {PersonaId}", incoming.PersonaId);
        }

        _settling.Begin(
            incoming.PersonaId,
            outgoing.PersonaId,
            outgoing.DisplayName,
            stimulus.StimulusId,
            SettlingState.DefaultSettlingTurns);
        _settling.MarkArrivalJournalWritten();

        var note =
            "Soft handoff complete. Prior transcript is another mind's archive. " +
            $"Identity/directives apply next chat turn; settling for {SettlingState.DefaultSettlingTurns} turn(s). " +
            $"stimulus_id={stimulus.StimulusId}";

        _logger.LogInformation(
            "Soft switch {From} → {To} stimulus={StimulusId}",
            outgoing.PersonaId,
            incoming.PersonaId,
            stimulus.StimulusId);

        return new SoftSwitchResult(
            Ok: true,
            Persona: incoming,
            StimulusId: stimulus.StimulusId,
            Note: note,
            SettlingTurnsRemaining: SettlingState.DefaultSettlingTurns);
    }
}
