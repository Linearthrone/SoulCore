---
type: report
from: QA-01
to: PM-01
prop_id: PROP-20-soulcore-persona-platform
status: Partial
completed: 2026-11-05
---

# QA — PROP-20 SoulCore persona platform

## Scope

Persona runtime + shared spaces + Identity wizard + Victoria→SoulCore rebrand on `feature/soulcore-persona-platform` → remote `Linearthrone/SoulCore`.

## Evidence

### Build

- `dotnet build SoulCore/SoulCore.Host/SoulCore.Host.csproj -c Debug` — **Pass**
- `dotnet build House/House.ChatDesktop/House.ChatDesktop.csproj -c Debug` — **Pass**

### Unit tests (filtered)

```
dotnet test SoulCore/SoulCore.Protocol.Tests --filter
  PersonaPackStoreTests|SharedSpaceStoreTests|PersonaTraitCompilerTests|
  PersonaStoreHubQuarantineTests|ChatContextBuilderTests|PersonaToolPathsTests
```

**Passed: 44 / Failed: 0**

### Checks covered

| Item | Result |
| --- | --- |
| Blank default; no victoria starter seeded | Pass (PersonaPackStoreTests) |
| Per-persona quarantine hub | Pass (PersonaStoreHubQuarantineTests) |
| Shared space upsert/recall + attach only when listed on pack | Pass (SharedSpaceStoreTests) |
| Trait compiler | Pass |
| ChatDesktop Identity tab + wizard compile | Pass |
| Product titles / Android package / SMS rename / LocalAppData SoulCore | Spot-checked Pass |

### Remaining / Partial

- Live Host E2E (create persona in UI, switch, chat) needs running Ollama + desk smoke on home PC.
- Release build blocked without SixLabors license (Debug OK) — pre-existing tooling constraint.
- Internal `IVictoria*` type/table names remain behind facades (documented tech debt).
- Push to `origin` (`Linearthrone/SoulCore`) pending after commit.

## Verdict

**Partial Pass** — code + unit quarantine/shared-space evidence green; operator desk smoke still required for full PROP-20.5 close.
