using Microsoft.Extensions.Logging.Abstractions;
using SoulCore.Core.Persona;
using SoulCore.Core.SharedSpaces;
using SoulCore.Host.Persona;
using SoulCore.Host.SharedSpaces;

namespace SoulCore.Protocol.Tests;

public class SharedSpaceStoreTests
{
    [Fact]
    public async Task Upsert_WriteFact_Recall_And_AttachToPersona()
    {
        var spacesRoot = Path.Combine(Path.GetTempPath(), "sc-spaces-" + Guid.NewGuid().ToString("N"));
        var personasRoot = Path.Combine(Path.GetTempPath(), "sc-personas-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(spacesRoot);
        Directory.CreateDirectory(personasRoot);
        try
        {
            var spaces = new SharedSpaceStore(spacesRoot, NullLogger<SharedSpaceStore>.Instance);
            var saved = await spaces.UpsertAsync(new SharedSpace
            {
                SpaceId = "house-facts",
                DisplayName = "House facts"
            });
            Assert.Equal("house-facts", saved.SpaceId);

            using (var mem = spaces.OpenMemory("house-facts"))
            {
                await mem.WriteFactAsync("The kettle is ceramic.", "test");
                var facts = await mem.RecallRecentAsync(5);
                Assert.Contains(facts, f => f.Contains("kettle", StringComparison.OrdinalIgnoreCase));
            }

            var packs = new PersonaPackStore(personasRoot, NullLogger<PersonaPackStore>.Instance);
            await packs.EnsureSeededAsync();
            var blank = await packs.GetAsync(PersonaPack.BlankPersonaId);
            Assert.NotNull(blank);
            blank!.SharedSpaceIds = ["house-facts"];
            await packs.UpsertAsync(blank);

            var reloaded = await packs.GetAsync(PersonaPack.BlankPersonaId);
            Assert.Contains("house-facts", reloaded!.SharedSpaceIds);

            var builder = new SharedSpaceContextBuilder(spaces);
            var block = await builder.BuildAttachedContextAsync(reloaded);
            Assert.Contains("house-facts", block, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("kettle", block, StringComparison.OrdinalIgnoreCase);

            // Quarantine: persona without attach sees nothing.
            var mentor = await packs.GetAsync(PersonaPack.MentorPersonaId);
            var empty = await builder.BuildAttachedContextAsync(mentor!);
            Assert.Equal(string.Empty, empty);
        }
        finally
        {
            try { Directory.Delete(spacesRoot, true); } catch { /* ignore */ }
            try { Directory.Delete(personasRoot, true); } catch { /* ignore */ }
        }
    }
}
