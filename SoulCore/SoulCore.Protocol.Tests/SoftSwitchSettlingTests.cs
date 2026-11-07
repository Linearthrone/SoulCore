using SoulCore.Core.Wellbeing;
using SoulCore.Host.Wellbeing;

namespace SoulCore.Protocol.Tests;

public class SoftSwitchSettlingTests
{
    [Fact]
    public void SettlingState_BlocksOtherPersona_UntilTurnsExhaust()
    {
        var state = new SettlingState();
        state.Begin("mentor", "blank", "Blank", "stim1", turns: 2);

        Assert.True(state.IsSettling);
        Assert.True(state.BlocksActivate("analyst", out var detail));
        Assert.Contains("settling", detail, StringComparison.OrdinalIgnoreCase);
        Assert.False(state.BlocksActivate("mentor", out _));

        Assert.Equal(1, state.NotifyChatCompleted("mentor"));
        Assert.True(state.IsSettling);
        Assert.Equal(0, state.NotifyChatCompleted("mentor"));
        Assert.False(state.IsSettling);
        Assert.False(state.BlocksActivate("analyst", out _));
    }

    [Fact]
    public void NarrativeMentalState_BodiesAreProseNotVADOnly()
    {
        var close = NarrativeMentalState.BuildOutgoingSwitchClose("Blank", "Mentor", "mentor");
        Assert.Contains("Soft handoff leaving", close, StringComparison.Ordinal);
        Assert.Contains("Impact:", close, StringComparison.Ordinal);
        Assert.DoesNotContain("valence=", close, StringComparison.Ordinal);

        var arrival = NarrativeMentalState.BuildIncomingSwitchArrival(
            "Mentor", "Blank", "blank", "abc123");
        Assert.Contains("Soft handoff arrival", arrival, StringComparison.Ordinal);
        Assert.Contains("Unsettled:", arrival, StringComparison.Ordinal);
        Assert.Contains("stimulus_id=abc123", arrival, StringComparison.Ordinal);

        var settle = NarrativeMentalState.BuildSettlingPreamble("Blank", "blank", 3);
        Assert.Contains("soft-switch settling", settle, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(NarrativeMentalState.JournalGuidance, settle, StringComparison.Ordinal);
    }
}
