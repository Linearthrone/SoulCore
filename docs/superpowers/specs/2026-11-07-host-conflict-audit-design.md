# Host conflict audit — decisions & remediation (2026-11-07)

PM-01 audit of `SoulCore.Host` for conflicts, redundancies, dead weight, and wording that fights the operator.

## Decision (A1 naming)

`PersonaPack.HumanAddress` is the standing name SoT. It is **not** hardcoded to Kayleigh.

- Desk chat and SMS detect corrections (`call me …`, `my name is …`, `stop calling me X, call me Y`, …) and **upsert + reload** the active pack so restarts cannot revive a dead name.
- Wake continuity, episodic factual records, emotion preamble, SMS preamble, and vision-attach lines all use the pack address.
- Prompt rule: pack address survives restarts; obsolete names in memory/transcript must not win.

## Phase A (prompt / identity) — done

| Item | Action |
| --- | --- |
| A1 | `HumanAddressCorrection` + persist path; labels use pack address |
| A2 | Removed misleading `Pack tool policy: desktop=on/off` prompt text |
| A3 | Chat uses `PersonaToolPathsResolver.ResolveDesktopTargetWindowTitle()` |
| A4 | `[HomeBody]` / `[ChiefArchitect]` intent-gated |
| A5 | ComputerUse safety coda once; ToolAgency defers SOP/site to memory blocks |

## Phase B (loops / gates) — done

| Item | Action |
| --- | --- |
| B1 | `IForegroundToolLoopGate.Register/Release` around WS tool-loop passes |
| B2 | SMS takes `WorkTurnGate` around inference |
| B3 | Shipped `SoulLoop:Enabled=false`; notes say Program does not force-override |

## Phase C (config / docs) — done

- Hermes key dropped from `--secrets-presence`
- PreferHermes / Hermes.Enabled “forced” notes scrubbed
- Browser/backend comments match desk `native` product
- `Inference:Model` corrected to `qwen2.5:14b`
- `/settings/tools` DTO note: backends startup-fixed; allow* session-mutable

## Out of scope

- Full ComputerUse pedagogy rewrite
- Scrubbing historical episodic rows that already contain a dead name (new writes + standing rule cover forward path)
