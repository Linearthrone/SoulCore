# SoulCore

Persona platform: .NET 8 Host, Presence ChatDesktop, tablet SMS gateway, optional Unreal embodiment.

**Product split:** This repository (`Linearthrone/SoulCore`) is the multi-persona SoulCore product. The House Victoria companion line remains on [`Linearthrone/SoulCore.AI`](https://github.com/Linearthrone/SoulCore.AI).

## Quick start (Windows home PC)

```powershell
.\ALLSTART.ps1
```

Stops with `.\ALLSTOP.ps1`.

## Documentation (searchable)

Operational + architecture handbook:

```bash
cd docs-site
npm install
npm run docs:dev
```

Then open the printed local URL and use the search box.

Source of truth lives under `docs/handbook/` (architecture, workflows, conventions).  
Day-to-day ops procedures: `docs/runbooks/`.  
Agent seats: `Agents/`.  
PROP registry: `docs/agents/PROP_NUMBERING.md`.

## Layout

| Path | Role |
| --- | --- |
| `SoulCore/` | Host, inference/tools, memory, protocol, tests |
| `House/House.ChatDesktop` | Presence desk UI |
| `House/House.CompanionAndroid` | SoulCore Link (phone) |
| `House/House.Voice` | STT/TTS helpers |
| `BrowserCaptureBridge/` | Native browser capture helper |
| `docs/handbook/` | Architecture & workflows (handbook site) |
| `docs/runbooks/` | Ops procedures |
| `docs/agents/` | PROP tickets / reports (historical work tracking) |
| `Agents/` | Role activation packs |
| `sms-*.sh` | Tablet Termux SMS bridge (temporary until self-sufficient gateway) |

## Direction

Satellite apps (Tasker, Termux:API glue) are a **temporary** bridge. End state: self-sufficient tablet gateway. See `docs/handbook/workflows/sms-gateway.md`.

## Personas

Each persona has a quarantined memory DB under `%LOCALAPPDATA%/SoulCore/personas/{id}/`. Named **shared spaces** (optional) live under `%LOCALAPPDATA%/SoulCore/shared-spaces/{spaceId}/` and are attached per pack at create/edit. See `docs/agents/PROP_SOULCORE_PERSONA.md` (PROP-20).
