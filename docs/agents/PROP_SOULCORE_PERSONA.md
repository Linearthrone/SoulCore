---
type: prop
prop_id: PROP-20-soulcore-persona-platform
status: executing
created: 2026-11-05
owner: PM-01
repo: Linearthrone/SoulCore
---

# PROP-20 — SoulCore persona platform (Victoria strip + shared spaces)

Successor execution of PROP-15 intent on **`Linearthrone/SoulCore`** (fork of `Linearthrone/SoulCore.AI`).

## Product split

| Repo | Product |
| --- | --- |
| `Linearthrone/SoulCore.AI` | House Victoria companion line (unchanged upstream) |
| `Linearthrone/SoulCore` | SoulCore persona platform — no Victoria brand/default |

## Locked

- Per-persona quarantined SQLite; Blank/Mentor/Analyst starters (no Victoria starter)
- Named **shared spaces** (separate stores); attach at create/edit
- Single active persona v1; Identity-tab UX in current Presence style
- Full rebrand: desk, Android, SMS, Velopack, AppData, docs/agents

## Splits

| Split | Owner | Work |
| --- | --- | --- |
| 20.0 | PM-01 | Repo contract + README + registry |
| 20.1 | BED-01 | Persona runtime port + adopt legacy DB |
| 20.2 | BED/DBD | Shared spaces store + API + context |
| 20.3 | FED-01 | Identity tab select/create/edit wizard |
| 20.4 | FED/BED/OPS | Full Victoria → SoulCore strip |
| 20.5 | QA/SLOP | Quarantine, spaces, rebrand evidence |

## TECHDEBT — PROP-20.4 rebrand leftovers (internal identifiers, not user-facing)

Commit `chore(brand): strip House Victoria -> SoulCore (PROP-20.4)` removed user-facing Victoria / House Victoria strings. These **internal** names were deliberately left (renaming risks DB/wire/VM breakage):

- C# types / interfaces: `IVictoria{Task,Workflow,Journal,Sop,Research}Store`, `Victoria*` records and `SqliteVictoria*Repository`, `VictoriaVmPointer*`, `VictoriaBrowser*` (Presence + Host), `VictoriaVirtualBoxWindowLocator`, `VictoriaEyeCaptureTool`, `EmailOptions.RoleVictoria`, `SmsOptions.VictoriaMdn` (+ `SOULCORE_Sms__VictoriaMdn`), `PersonaPack.VictoriaPersonaId`, `PersonaWizardLogic.RequiresVictoria` and the `"victoria"` legacy pack id.
- SQLite tables / migrations: `victoria_tasks`, `victoria_workflows`, `victoria_journal_*`, `victoria_sops`, `victoria_research_*`, `victoria_instances` and `*_victoria_*.sql` embedded-resource names.
- VirtualBox VM is still registered as `victoria-sandbox` (guest user `victoria`): `LocalStackControl.SandboxVmName`, `Tools:DesktopTargetWindowTitle`, `force-unlock-victoria-sandbox.ps1`, `.env.example`. UI/tooltips/LLM guidance now say **SoulCoreVM**; `ComputerUseGuidance` intent regex accepts both names. Rename VM + constant + script + config together in a follow-up OPS/VBOX task.
- Unreal / infra: `house-victoria` Tailscale host, `HouseVictoriaBridge` plugin, `GM_HouseVictoria`, `BP_*Victoria*`, `VictoriaAvatar`, `victoria_eye_capture` tool name, `House/UnrealBridge/README.md`.
- Android: `VictoriaLinkShell.kt` class name, notification channel ids `victoria_replies` / `victoria_connected`, mailbox id special-case `"victoria"` (label now "Companion's mailbox").
- Voice: `House.Voice/tts/server.py` voice alias key `"victoria"`.
- Presence update feed still accepts legacy `HouseVictoria.Presence-*` asset names (new pack id: `SoulCore.Presence`); old `%LocalAppData%\HouseVictoria` data is not migrated (new path: `%LocalAppData%\SoulCore`).
- Defaults: `Companion:DefaultContactId/Name` = `blank` / `Blank`; Presence `LocalUiSettings.DefaultDisplayName` = `Companion`.