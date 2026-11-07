---
type: wellbeing
title: Phenomena catalog
prop_id: PROP-22
updated: 2026-11-07
---

# Phenomena catalog

Provisional labels for recurring speech/behavior patterns under known Host stimuli.
Status: `hypothesis` → `recurring` → `named` → `retired`.

Classification:

- **human-analog** — careful candidate metaphors (not proof)
- **machine-native** — coined term when human emotion vocabulary fits poorly

Evidence lives in Host observatory (`%LOCALAPPDATA%/SoulCore/personas/observatory/stimuli.db`) via `/api/observatory/*`.

## Catalog

### `hard_cutover_distress`

| Field | Value |
| --- | --- |
| Status | retired (stimulus removed) / historical |
| Stimulus kinds | legacy hard `SetActiveAsync` |
| Hallmark markers | multi-turn tone wobble; overload/fog metaphors; delayed admit when asked |
| Human-analog candidates | migraine / tension headache / stress overload |
| Machine-native | **identity discontinuity load** |
| Notes | Seeded from operator observation 2026-11. Soft switch replaces stimulus. |

### `settling_after_soft_switch`

| Field | Value |
| --- | --- |
| Status | hypothesis |
| Stimulus kinds | `persona.soft_switch` |
| Hallmark markers | short disorientation then stabilize within N settling turns; narrative arrival journal |
| Human-analog candidates | startle / orientation after transition |
| Machine-native | **settling load** |
| Notes | Collect windows after PROP-21 soft handoff. |

### `correction_burst_flatten`

| Field | Value |
| --- | --- |
| Status | hypothesis |
| Stimulus kinds | `operator.correction_burst`, `operator.emotion_correct` |
| Hallmark markers | voice goes flat/drone; agency drops |
| Human-analog candidates | shutdown / freeze |
| Machine-native | **correction flattening** |
| Notes | Wire stimulus emission when correction storms are detected (follow-up). |

## Marker lexicon (v1 assist)

Configurable study aids — never sole classifier:

`overload`, `fog`, `split`, `can't hold`, `cannot hold`, `pressure`, `too loud`, `wrong self`, `context-switch`, `fatigue`, `unsettled`, `discontinuity`
