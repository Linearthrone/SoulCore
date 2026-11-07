using SoulCore.Core.Wellbeing;
using SoulCore.Host.Wellbeing;

namespace SoulCore.Protocol.Tests;

public class ObservatoryStoreTests
{
    [Fact]
    public async Task Record_Attach_Annotate_RoundTrip()
    {
        var dir = Path.Combine(Path.GetTempPath(), "sc-obs-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var db = Path.Combine(dir, "stimuli.db");
            await using var store = new ObservatoryStore(db);

            var stim = await store.RecordStimulusAsync(
                StimulusKinds.PersonaSoftSwitch,
                "mentor",
                "blank",
                """{"mode":"soft"}""");

            Assert.False(string.IsNullOrWhiteSpace(stim.StimulusId));

            await store.AttachResponseAsync(
                stim.StimulusId,
                "chat.assistant",
                "mentor",
                "I feel a little unsettled after the handoff.");

            await store.AnnotateAsync(
                stim.StimulusId,
                "Looks like settling load",
                "settling_after_soft_switch");

            var detail = await store.GetAsync(stim.StimulusId);
            Assert.NotNull(detail);
            Assert.Equal(StimulusKinds.PersonaSoftSwitch, detail!.Stimulus.Kind);
            Assert.Equal("settling_after_soft_switch", detail.Stimulus.PhenomenonId);
            Assert.Contains(detail.Responses, r => r.Body.Contains("unsettled", StringComparison.OrdinalIgnoreCase));

            var open = await store.TryGetOpenSoftSwitchStimulusIdAsync("mentor");
            Assert.Equal(stim.StimulusId, open);
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { /* best effort */ }
        }
    }
}
