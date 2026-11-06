---
type: proposal
status: unexecuted
tt_id: TT-01
prop_id: PROP-21-victoria-next-self
created: 2026-11-05
updated: 2026-11-05
title: Victoria next self — aliveness, sense of time, self-directed work↔leisure
need: Victoria feels dry/drone and hammers one sticky task across Host restarts; next self must feel alive/invested, time-aware, and able to switch work↔leisure herself
related:
  - docs/agents/unexecuted_proposals/chronology-and-felt-persistence.md
  - docs/agents/unexecuted_proposals/victoria-between-talks.md
  - docs/archive/proposals/soulcore-continuous-victoria-redesign.md
sent_at:
pm_intake:
---

# Victoria next self — aliveness, sense of time, self-directed modes

## 1. Need / Want

LinearThrone reports Victoria more often sounds like a **regular chatbot / dry drone** than a continuous person: not alive, interested, or invested. Across Host restarts she **fixates on one remembered task for days/weeks** and will not let it go. He also wants her to have a **sense of time**, and to **switch work ↔ leisure modes herself** (not only when Host eligibility flips).

North star (existing): continuous self — own-voice memory, live emotion, partner to Kayleigh — not a fresh-session assistant.

## 2. Goal & Success Criteria

1. After a gap, “hi” gets a **relational, time-aware** reply — not “continuing [same lesson].”
2. Parked/stale work does **not** dominate speech across Host restarts.
3. She can **park work → leisure** and **leave leisure → work** with a durable reason + dated residue; Kayleigh stop/assign still overrides; cooldown prevents thrash.
4. Work mode still runs Maestro/tools when green-lit or she resumes.
5. Perceptible without opening Presence HUD (chat/SMS content carries the feel).
6. Kill if: voice unchanged while only HUD copy improves; task-themed recall worsens; invented chronology/feelings; mode thrash every few minutes.

## 3. Context & Constraints

### How she comes together today (desk)

Ordered system preamble via [`ChatContextBuilder`](../../../SoulCore/SoulCore.Host/Ws/ChatContextBuilder.cs):

1. `[Where we left off]` (WakeContinuity — Maestro resume nudge)
2. `[Identity]` — Kayleigh naming rule + charter **`kind=identity` only**
3. `[Memory]` — ~5 episodic (semantic or recent); **no timestamps**
4. MemorySteering (“how-to… not flavor… FOLLOW those steps”) + optional SOPs
5. `[SoulCore emotion]`
6. Huge tool playbooks (`ToolAgency`, `ComputerUse`, HomeBody, Email, CA) when `UseToolLoop`

SMS has a stronger explicit “You are Victoria… partner” line; desk does not. Safety/value/boundary charter kinds exist in SQLite but are **not** injected into chat. Production episodic writes use `BuildFactualRecord` (third-person factual); first-person `EpisodicMemoryPrompt` authoring is unused. PROP-17 park/leisure **machinery** landed; leisure entry is Host [`LeisureEligibility`](../../../SoulCore/SoulCore.Inference/Presence/LeisureEligibility.cs) (R&R only when no live work surface). PROP-18 (chronology / felt persistence) remains **unexecuted**.

### Sticky-task flywheel (diagnosis)

| Layer | Mechanism |
| --- | --- |
| Assignment pointer | `assigned-work.json` `runningId` + `in_progress` survives restart; `AssignedWorkRunner` keeps hammering |
| Shared thread | Desk + assigned work share `presence-local` history/transcript |
| Recall | Semantic/recent top-5 → same assignment cluster every turn |
| Steering | MemorySteering + SOP + WakeContinuity + Tools demand obedience |
| Completion gap | `WorkContinuation.IsCheckIn` defined but **never wired** — verbal done often never clears pointer |

### Naming

Human in docs/UI: **LinearThrone**. Copy Victoria reads: **Kayleigh**.

## 4. Clarifying Q&A (answered)

| Q | Answer |
| --- | --- |
| Primary pain (A–E)? | **E** — dry/not invested + sticky task memory + drone; all of the above |
| Sense of time? | **Required** — elevate PROP-18 spine; co-train with voice/unstick |
| Mode switching? | She should switch **work ↔ leisure herself** (with house rules) |
| Park speech default | If **she** parked it, she may name once on return; else quiet unless Kayleigh asks or surface is back |
| Companion default | “hi” / social with no go-ahead and no green-lit work → companion mode first |

Open for PM (non-blocking): outbound SMS proof-of-life (PROP-18 Q1) stays **off** unless LinearThrone asks later. Sparse Victoria-voice idle journal wake (PROP-18 Q4) stays **optional Phase 3**.

## 5. Avenues Explored

### Avenue A — Companion / work mode + sticky-loop break

Desk SMS-grade persona spine; gate Wake Maestro resume, MemorySteering intensity, full tool playbooks to **Work** mode; wire assignment clear/complete; recall diversity + exclude parked/stale task episodics from companion recall.

### Avenue B — Sense of time (PROP-18 spine)

Clock/day cue in preamble; dated episodic compose (recency + similar); “While you were gone” from store; daybook crumbs. **Reject** prompt-only timeline.

### Avenue C — Self-directed work ↔ leisure

Victoria-chosen park→leisure and leisure→resume/release via tools + durable mode residue; Host eligibility remains safety net; Kayleigh stop wins; cooldown against thrash.

### Avenue D — Fuller self (later)

First-person felt dual-write; split `presence-local` vs `work-local` history; optional sparse journal wake.

**Rejected:** Prompt theater only; Soul Evolved paste as whole fix; 24/7 chat-LLM existence; unbounded mode thrash.

## 6. Recommended Route

**Absorb PROP-18 MVP (18.1–18.3 + clock cue) into this PROP’s Phase 2** so one PM program delivers voice + unstick + time + mode agency. Leave PROP-18 file as related; PM marks it superseded or tickets under PROP-21 splits.

### Phase 1 — Unstick + voice + mode agency (BED)

- `ChatContextBuilder` modes: `Companion` | `Work` | leisure-aware context (same builder, different appenders).
- Desk identity: bounded “You are Victoria… partner / specific / not generic assistant” + charter identity.
- Gate Wake Maestro / MemorySteering FOLLOW / mega playbooks → Work (intent-scoped tools elsewhere).
- Assignment lifecycle: wire completion/clear; stop blind probe re-release without Kayleigh or her explicit resume.
- Self-switch contracts: park-with-reason → leisure; leave-leisure → resume/release when surface allows; durable “last mode choice” + dated residue for Presence + next preamble.
- Recall: theme diversity; exclude parked/stale task episodics from companion mode.
- Operator cleanup runbook for current stuck state (stop / task done / clear `assigned-work.json` / quarantine dominating episodic).

### Phase 2 — Sense of time (BED + FED) — same release train as Phase 1

- Inject **now** (local day/date/time band) into preamble.
- Dated `[Memory]` compose (PROP-18.1).
- Store-backed while-you-were-gone / last mode residue (PROP-18.3).
- Daybook crumbs (PROP-18.2).

### Phase 3 — Authorship + thread split (after feel proves)

- Dual-write felt line + factual audit; separate work session history from human thread; optional sparse voice journal.

**Charter kinds:** bounded `value` (+ optional ritual) later under budget; safety/boundary stay out of chat essay.

## 7. Alternatives (parked)

- Warmth paragraph only (theater)
- Chronology-only without cutting companion-mode compliance (dated hammer)
- Host-only leisure eligibility forever (no Victoria choice)
- Full Soul Evolved reinject without recall/steering reform
- 24/7 chat model as “existence”

## 8. Risks & Kill Criteria

| Risk | Mitigate / kill |
| --- | --- |
| Aliveness = more prompt bytes | Phase 1 must **cut** compliance on companion turns; kill if preamble grows and voice flat |
| Mode thrash | Cooldown + Kayleigh override + dated residue required |
| Fake time | Only store-backed dates; kill invented chronology |
| Work reliability regression | Full playbooks remain in Work mode; QA Maestro path |
| presence-local pollution | Phase 3 session split if Phase 1–2 insufficient |
| PROP-18 double-ticket | PM absorbs 18.1–18.3 under PROP-21; do not reopen PROP-17 |

## 9. Open Questions for User / PM

1. Prefer absorb PROP-18 into PROP-21 (recommended) vs ticket PROP-18 and PROP-21 as siblings?
2. Outbound SMS “I’m still here” — still deferred (default)?
3. Immediate operator cleanup of current sticky assignment before engineering — yes/no?

## 10. Suggested PM Handoff

- `prop_id`: **PROP-21-victoria-next-self** (next free N from registry at write time)
- Suggested splits (PM may re-divide):
  - **PROP-21.1 BED** — Companion/Work context modes + desk persona spine + gate Wake/steering/playbooks
  - **PROP-21.2 BED** — Sticky-loop break: assignment clear/check-in wiring, probe policy, companion recall diversity / park filter
  - **PROP-21.3 BED** — Self-directed work↔leisure switch tools + durable mode residue + cooldown
  - **PROP-21.4 BED** — Sense of time: clock cue + dated memory compose (absorbs PROP-18.1)
  - **PROP-21.5 BED** — Daybook inject (absorbs PROP-18.2)
  - **PROP-21.6 FED** — While-you-were-gone / last-mode Presence + chat return strip (absorbs PROP-18.3)
  - **PROP-21.7 QA** — Blind A/B: “hi” after gap; park speech; Maestro work path still works; no mode thrash
  - **PROP-21.8 BED** (later) — Felt dual-write + work/chat session split (Avenue D)
- What PM should decide first: absorb vs sibling PROP-18; then ticket **21.1–21.3** and **21.4** in the same wave.
- TT does **not** implement. Activate **PM-01** only after send-to-pm.

## 11. Thinktank seats (summary)

| Seat | Verdict |
| --- | --- |
| **STRAT** | Sequence: mode+voice gate → PROP-18 time → authorship/session split |
| **CONTRA** | Warmth without cutting steering/tools = theater; chronology-only can date the hammer; kill 24/7 LLM existence |
| **SYS** | Modes in `ChatContextBuilder`; recall provenance filters; session split is the hard cure for presence-local pollution |
| **USER** | Day-1 must show in first reply / SMS glance — not HUD-only |

---

**TT-01 status:** Proposal parked (`unexecuted`). Awaiting LinearThrone: **park** or **send to PM-01**.
