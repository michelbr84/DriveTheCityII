# Audio

| Field | Value |
|---|---|
| ID | SPEC-AUDIO |
| Document status | Draft |
| Release scope | v1.0 |
| Phases | PHASE-02 or PHASE-04 (audio integration probe, OPEN-304), PHASE-06 (mission cues), PHASE-11 (complete set and mix), PHASE-13 (provenance audit) |
| Responsible role | Coordinator (maintainer); Implementer; project owner approves any audio source; Reviewer checks provenance |
| Related requirements | REQ-AUDIO-001, REQ-INPUT-003, REQ-ART-002, REQ-TIME-002, REQ-REL-003, REQ-PERF-001 |
| Related documents | [ASSET_PROVENANCE](../legal/ASSET_PROVENANCE.md), [SPEC-UI](UI_AND_HUD.md), [SPEC-INPUT](CONTROLS_AND_ACCESSIBILITY.md), [SPEC-ART](ART_AND_ANIMATION.md), [SPEC-DRIVING](../gameplay/VEHICLE_DRIVING.md), [SPEC-MISSION](../gameplay/MISSION_FRAMEWORK.md), [SPEC-PERF](../architecture/PERFORMANCE_BUDGET.md) |
| Last updated | 2026-10-18 |

> **Status:** plan only. No sound has been created, licensed, or played. Whether and how the mod can play its own
> positional sounds and respect the game's volume settings is unverified (OPEN-304).

## 1. Purpose

This spec defines the mod's sounds, where they may come from, how they mix with the game's own audio, which cues
need captions, and which audio settings the player gets.

## 2. Scope and non-goals

**In scope:** footsteps, vehicle engine, tires, impacts, door and transition sounds, UI sounds, and mission cues.

**Non-goals for v1.0 [Proposal]:**

- Voice acting for passengers or mission characters. Their lines are text.
- A music soundtrack.
- Replacing or altering the game's own sounds, including traffic and ambience.
- Changing the player's game audio settings.

## 3. Behavior and contracts

### 3.1 Sound catalog **[Proposal]**

| Group | Sound | Trigger | Variation | Spatial | Caption |
|---|---|---|---|---|---|
| Footsteps | Step | Walk or run cycle (`OnFoot`) | At least two surface families (hard paving, soft ground), several takes each | 3D, at the avatar | No |
| Engine | Idle, load, off-load | `Driving`; pitch and blend follow engine speed and throttle | One set per vehicle class (car, light delivery, rigid truck, bus) | 3D, at the vehicle | No |
| Engine | Start / stop | Entering or leaving `Driving` | Per class | 3D | No |
| Tires | Rolling | Speed above a threshold | Surface family | 3D | No |
| Tires | Skid | Grip loss or hard braking | Intensity levels | 3D | Optional ("[Tires screech]") |
| Impacts | Light / heavy | Contact with a static obstacle or a vehicle ([SPEC-CONTACT](../architecture/WORLD_CONTACT.md)) | Intensity and material | 3D | Yes, heavy only ("[Collision]") |
| Vehicle | Door or transition | Entry and exit (real or substitute transition) | Per class | 3D | No |
| Vehicle | Parking brake, reverse alert | Toggle; reversing in heavy classes | — | 3D | No |
| UI | Confirm, back, error or blocked | Menus and prompts | — | 2D | No (the visual feedback already exists) |
| UI | Payment | Settlement | Positive and neutral variants | 2D | No (the summary is shown) |
| Mission | Offer available | New offer matching the current career | — | 2D | Yes ("[New job offer]") |
| Mission | Passenger boarded / cargo loaded | Leg reaches `Servicing` done | — | 2D | Yes |
| Mission | Destination near | Within a set distance of the access point | — | 2D | Yes |
| Mission | Deadline warning | Deadline threshold reached | Escalates once | 2D | Yes ("[Deadline soon]") |
| Mission | Job completed / failed / cancelled | Terminal outcome | Distinct per outcome | 2D | Yes |
| Mission | Route updated | Reroute after a wrong turn or a city edit | — | 2D | Yes |
| System | Low fuel | Fuel threshold | Two levels | 2D | Yes ("[Low fuel]") |
| System | Recovery | `Recovery` starts and ends | — | 2D | Yes |

"Caption: Yes" marks an important cue (REQ-INPUT-003). Every important cue also has a visual notice in
[SPEC-UI](UI_AND_HUD.md), so no information depends on sound alone.

### 3.2 Sourcing policy (REQ-AUDIO-001)

- **Allowed:** sounds recorded or synthesized by the project, and third-party sounds whose license allows
  modification and redistribution in a game mod. Each one gets a provenance entry in
  [ASSET_PROVENANCE](../legal/ASSET_PROVENANCE.md) with source, author, license, date, and modifications.
- **Generated audio:** allowed only with project-owner approval, recorded like any other source.
- **Forbidden:** audio extracted from CS2, Motor Town, or any other game; recordings of copyrighted music; sounds of
  unknown origin.
- **Placeholders** are allowed during development, labeled and listed like visual placeholders
  ([SPEC-ART](ART_AND_ANIMATION.md) section 3.5, REQ-ART-002).
- License compatibility with the eventual project license is checked in
  [REUSE_AND_LICENSE_AUDIT](../legal/REUSE_AND_LICENSE_AUDIT.md) (OPEN-027).

### 3.3 Mixing with game audio

**Rules [Proposal]**, subject to what OPEN-304 finds possible:

1. The city's own ambience and traffic keep playing. The mod adds sounds on top and never mutes the city.
2. Mod sounds should follow the game's master volume and, where available, an effects or interface category. If the
   mod cannot use the game's categories, it provides its own volume sliders (section 3.5), and its defaults are set so
   the mod is not louder than the game.
3. The engine is the loudest continuous mod sound in `Driving`, but it stays below important cues. Important cues
   briefly lower the engine and tire sounds (ducking of about 30%, for less than 1 s).
4. In `Building` mode, mod world sounds stop. Only UI and notification sounds may play.
5. In `Paused`, mod world sounds pause or fade out, and resume with the game (REQ-TIME-002).
6. Focus loss follows the game's behavior for its own audio. The mod does not keep playing alone.
7. The number of sounds playing at once is bounded (for example, at most 16 mod voices) and is part of the
   performance measurements ([SPEC-PERF](../architecture/PERFORMANCE_BUDGET.md), REQ-PERF-001).

### 3.4 Captions

- Captions are on by default for important cues. They can be turned off, resized, and given a background panel
  ([SPEC-INPUT](CONTROLS_AND_ACCESSIBILITY.md) section 3.7).
- Format: a short bracketed description, such as `[Deadline soon]`. When a direction is relevant, an arrow is added
  (for example, `[Collision ←]`).
- Caption text is externalized under `dtc2.caption.*` keys and translated ([SPEC-L10N](LOCALIZATION.md)).
- Passenger and mission-character lines are text, not voice, in v1.0. They appear in the same caption area.

### 3.5 Settings

| Setting **[Proposal]** | Default |
|---|---|
| Mod master volume | 80% |
| Engine and tires | 80% |
| Impacts and effects | 80% |
| UI and mission cues | 80% |
| Captions for important cues | On |
| Caption size | Medium |
| Reduce repetitive sounds (fewer footstep and tire-roll sounds) | Off |

## 4. Failure handling and recovery

- **Missing sound file or failed playback:** silence for that sound, one log entry per sound per session, and no
  crash. The caption and visual notice still appear.
- **Audio subsystem unavailable:** gameplay continues silently, the mod shows a one-time notice, and captions are
  forced on.
- **Runaway sounds** (for example, an engine loop left playing after an exit or unload): every looping sound is owned
  by an entity or state, and is stopped when that owner ends. This is checked in the unload tests
  (REQ-ENTITY-003).

## 5. Acceptance criteria

| Test | Criterion | Validation |
|---|---|---|
| TEST-AUDIO-001 | Footstep, engine, tire, and impact sounds play in their states, and every one has a provenance entry from an own or authorized source | Not run |
| TEST-INPUT-003 | Every "Caption: Yes" cue in section 3.1 shows a caption and a visual notice | Not run |
| TEST-ART-002 | Placeholder sounds are listed and labeled | Not run |
| TEST-REL-003 | Audio provenance and notices are complete before distribution | Not run |

## 6. Open questions and risks

| ID | Summary |
|---|---|
| OPEN-304 (new) | Whether the mod can play positional sounds and respect the game's volume, pause, and focus behavior. |
| OPEN-027 | Project license, which affects compatible audio licenses. |
| RISK-304 (new) | Authorized engine, tire, and impact sounds may be unavailable or of poor quality. |
| RISK-015 | IP misuse. |

Full entries: [RISKS_AND_OPEN_QUESTIONS](../planning/RISKS_AND_OPEN_QUESTIONS.md).
