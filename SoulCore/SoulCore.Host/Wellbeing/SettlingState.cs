namespace SoulCore.Host.Wellbeing;

/// <summary>PROP-21.1 — post soft-switch settling window (single-active).</summary>
public sealed class SettlingState
{
    private readonly object _gate = new();
    private string? _personaId;
    private string? _fromPersonaId;
    private string? _fromDisplayName;
    private string? _stimulusId;
    private int _remainingTurns;
    private bool _arrivalJournalWritten;

    public const int DefaultSettlingTurns = 3;

    public bool IsSettling
    {
        get
        {
            lock (_gate)
                return _remainingTurns > 0 && !string.IsNullOrWhiteSpace(_personaId);
        }
    }

    public SettlingSnapshot? TryGetSnapshot()
    {
        lock (_gate)
        {
            if (_remainingTurns <= 0 || string.IsNullOrWhiteSpace(_personaId))
                return null;
            return new SettlingSnapshot(
                _personaId!,
                _fromPersonaId ?? "",
                _fromDisplayName ?? "",
                _stimulusId ?? "",
                _remainingTurns,
                _arrivalJournalWritten);
        }
    }

    public void Begin(
        string incomingPersonaId,
        string fromPersonaId,
        string fromDisplayName,
        string stimulusId,
        int turns = DefaultSettlingTurns)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(incomingPersonaId);
        lock (_gate)
        {
            _personaId = incomingPersonaId.Trim();
            _fromPersonaId = fromPersonaId?.Trim() ?? "";
            _fromDisplayName = string.IsNullOrWhiteSpace(fromDisplayName) ? _fromPersonaId : fromDisplayName.Trim();
            _stimulusId = stimulusId?.Trim() ?? "";
            _remainingTurns = Math.Max(1, turns);
            _arrivalJournalWritten = false;
        }
    }

    public void MarkArrivalJournalWritten()
    {
        lock (_gate)
            _arrivalJournalWritten = true;
    }

    /// <summary>Call after a successful assistant reply while settling; returns remaining turns after decrement.</summary>
    public int NotifyChatCompleted(string activePersonaId)
    {
        lock (_gate)
        {
            if (_remainingTurns <= 0
                || !string.Equals(_personaId, activePersonaId, StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            _remainingTurns = Math.Max(0, _remainingTurns - 1);
            if (_remainingTurns == 0)
            {
                _personaId = null;
                _stimulusId = null;
            }

            return _remainingTurns;
        }
    }

    public bool BlocksActivate(string requestedPersonaId, out string? detail)
    {
        lock (_gate)
        {
            if (_remainingTurns <= 0 || string.IsNullOrWhiteSpace(_personaId))
            {
                detail = null;
                return false;
            }

            if (string.Equals(_personaId, requestedPersonaId, StringComparison.OrdinalIgnoreCase))
            {
                detail = null;
                return false;
            }

            detail =
                $"Soft-switch settling in progress for '{_personaId}' " +
                $"({_remainingTurns} turn(s) remain). Wait before activating another persona.";
            return true;
        }
    }
}

public sealed record SettlingSnapshot(
    string PersonaId,
    string FromPersonaId,
    string FromDisplayName,
    string StimulusId,
    int RemainingTurns,
    bool ArrivalJournalWritten);
