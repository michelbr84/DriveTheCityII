# Localization

| Field | Value |
|---|---|
| ID | SPEC-L10N |
| Document status | Draft |
| Release scope | v1.0 |
| Phases | PHASE-01 (externalization from the first string; mechanism spike, OPEN-026), PHASE-02 – PHASE-10 (continuous), PHASE-11 (complete PT-BR, review), PHASE-12 (string freeze, language checks in beta) |
| Responsible role | Coordinator (maintainer); Implementer (UI module); PT-BR linguistic reviewer (OPEN-305); QA |
| Related requirements | REQ-L10N-001, REQ-UI-001 – REQ-UI-004, REQ-ONBOARD-001, REQ-INPUT-003 |
| Related documents | [SPEC-UI](UI_AND_HUD.md), [SPEC-ONBOARD](ONBOARDING.md), [SPEC-AUDIO](AUDIO.md), [CODING_STANDARDS](../engineering/CODING_STANDARDS.md), [DOCUMENTATION_RULES](../DOCUMENTATION_RULES.md) |
| Last updated | 2026-10-18 |

> **Status:** plan only. No strings exist yet. How a CS2 code mod supplies localized strings is unverified
> (OPEN-026). The candidate base mod references a game localization assembly **[Source-inspected]**
> ([BASE_MOD_AUDIT](../research/BASE_MOD_AUDIT.md)). That is a lead to investigate, not a confirmed mechanism.

## 1. Purpose

This spec defines how all player-facing text is externalized and delivered in English and Brazilian Portuguese
(PT-BR): key naming, formatting of numbers, money, and time, plural handling, layout tolerance, pseudo-localization
testing, and the translation review workflow.

## 2. Scope and non-goals

**In scope:** every string a player can see or hear described: HUD, prompts, menus, notifications, feedback,
tutorial, hints, captions, settings, error messages, honesty labels, and binding names shown in prompts.

**Out of scope:**

- Project documentation, which is English only ([DOCUMENTATION_RULES](../DOCUMENTATION_RULES.md)).
- Log messages and diagnostics bundles, which stay in English for support.
- Languages other than English and PT-BR in v1.0. Adding a language later must need no code change.

## 3. Behavior and contracts

### 3.1 Languages and selection

| Locale | Role |
|---|---|
| `en` (English) | Source language and fallback |
| `pt-BR` (Brazilian Portuguese) | Full translation required for v1.0 (REQ-L10N-001) |

**Selection [Proposal]:** follow the game's language when it is English or Portuguese (Brazil). For any other game
language, use English. A mod setting can override this. Whether the mod can read the game's language and react to
changes is OPEN-026.

### 3.2 Externalization from PHASE-01

- From the first visible string in PHASE-01 (for example, the safe-disable setting), no player-facing text is
  hard-coded. This applies even to prototypes and debug-visible player text.
- Code refers to keys only. A missing key falls back to English and is logged once per key per session.
- No string concatenation to build sentences. Use whole-sentence templates with named placeholders, because word
  order differs between English and PT-BR.
- No text inside images or icons.
- Binding names in prompts come from the input system's display names, or from keys, never from hard-coded labels
  ([SPEC-INPUT](CONTROLS_AND_ACCESSIBILITY.md)).

### 3.3 Key naming convention

Format: `dtc2.<area>.<context>.<item>[.<part>]`

- `dtc2` is a fixed namespace. It stays the same even if the working title changes.
- Every segment is lowercase `snake_case` (`[a-z0-9_]+`).
- `<area>` comes from a closed list: `hud`, `prompt`, `menu`, `feedback`, `notify`, `mission`, `career`, `econ`,
  `garage`, `company`, `challenge`, `tutorial`, `hint`, `caption`, `settings`, `input`, `label`, `error`.
- `<part>` is used for multi-part texts: `title`, `body`, `button`, `tooltip`.
- Keys are never reused for a different meaning. A changed meaning means a new key, and the old one is retired.

| Example key | English source (illustrative) |
|---|---|
| `dtc2.prompt.enter_vehicle` | Enter vehicle |
| `dtc2.feedback.exit_blocked.body` | No safe place to get out here. |
| `dtc2.feedback.unaffordable.body` | Not enough money: {needed} needed, {available} available. |
| `dtc2.label.managerial_simulation` | Managerial simulation |
| `dtc2.caption.deadline_warning` | [Deadline soon] |
| `dtc2.tutorial.rent_car.body` | Walk to the rental desk and rent a basic car. |

### 3.4 Placeholders, plurals, and agreement

- Placeholders are named (`{fare}`, `{count}`, `{destination}`), never positional. Each placeholder has a declared
  type: money, distance, speed, duration, count, or text.
- **Plurals:** messages with counts use the plural categories of the Unicode CLDR rules for each locale, not "add an
  s". Portuguese and English differ for zero and fractional values in CLDR, so check the current rule data when
  choosing the mechanism. For counts that can be zero, prefer a dedicated zero message ("No offers right now"). If the
  chosen mechanism has no plural support, use `.one` / `.other` key parts as a fallback.
- **Grammatical gender and agreement:** PT-BR adjectives and articles agree with nouns. Avoid inserting nouns into
  templates where agreement would change. Career passengers are referred to with neutral wording.

### 3.5 Numbers, money, distance, and time

| Value | Rule **[Proposal]** |
|---|---|
| Integers and decimals | Locale formatting. English uses `,` for thousands and `.` for decimals; PT-BR uses `.` for thousands and `,` for decimals. |
| Money (wallet, ledger, prices) | Locale number formatting plus one wallet currency presentation used everywhere. Whether it shares the city budget's currency sign is OPEN-306. The wallet is always labeled so it cannot be mistaken for the city budget (REQ-ECON-001). |
| Speed and distance | km/h and km, or mph and miles, from a mod setting (default metric). The mod follows the game's unit preference if it can read one (OPEN-303). |
| Durations (deadlines, timers) | `m:ss` under one hour; locale wording for longer durations. |
| In-game time in the ledger | Uses the game's in-game date or time representation if available, otherwise a session-relative label. Real-world timestamps are not shown to the player. |

Formatting is done by one shared formatter in the UI module, so that the HUD, ledger, and summaries never disagree.

### 3.6 Text expansion and layout

- Lay out for at least **+35%** length over English for short labels, and +30% for sentences **[Proposal]**. PT-BR
  text is commonly longer than English.
- Containers wrap or grow. Fixed-width text boxes are not allowed for translatable text. If truncation is
  unavoidable, use an ellipsis plus a tooltip or a detail view, never a silent cut.
- Glyph coverage: the chosen font must render PT-BR diacritics (acute, grave, circumflex, and tilde accents, and the
  cedilla) in all weights used. This is checked in the PHASE-01 spike.

### 3.7 Pseudo-localization testing

- A generated pseudo-locale wraps each string in start and end markers, replaces letters with accented look-alikes,
  and pads the text by 40%. For example, "Enter vehicle" is displayed between markers, with accented look-alike
  letters, and about 40% longer.
- It reveals:
  - hard-coded text (no markers);
  - concatenation (broken markers);
  - truncation (a missing end marker);
  - missing glyphs.
- It runs in the PHASE-01 spike and on every UI change after that. Results are recorded as test evidence
  ([evidence](../quality/evidence/README.md)).

### 3.8 Translation and review workflow

1. **Author:** a new or changed English string gets its key and a context note (where it appears, the maximum
   length if any, what the placeholders mean). The key's status becomes `new` or `changed`.
2. **Translate:** PT-BR translation, using the project glossary. The glossary is kept with the future translation
   resources, not in this documentation. It aligns with the game's own PT-BR terminology where it overlaps (OPEN-305).
3. **Review:** a native PT-BR reviewer checks meaning, tone, agreement, and length (OPEN-305).
4. **In-context check:** screenshots or a play session in PT-BR confirm layout and meaning. Recorded as evidence.
5. **Approve:** the status becomes `approved`. Strings that are `changed` but not yet approved fall back to English
   in release builds, and are listed in the release checklist.
6. **String freeze:** before the PHASE-12 beta, only fixes change strings. Every change after the freeze repeats
   steps 2 to 5.

## 4. Failure handling and recovery

- **Missing key or translation:** use English and log it. Never show a raw key outside pseudo-localization builds.
- **Formatting error** (for example, a placeholder type mismatch): show the English template with the raw value and
  log it. Never crash.
- **Localization mechanism unavailable at runtime:** the mod runs in English with a one-time notice. Gameplay is
  unaffected.

## 5. Acceptance criteria

| Test | Criterion | Validation |
|---|---|---|
| TEST-L10N-001 | All player-facing text is externalized; complete English and PT-BR sets exist and display correctly in every screen of SPEC-UI and SPEC-ONBOARD | Not run |
| TEST-L10N-001 (pseudo-locale run) | The pseudo-locale shows no hard-coded text, broken concatenation, truncation, or missing glyphs | Not run |
| TEST-UI-003 | Blocked-action messages read correctly in both languages, including placeholders and plurals | Not run |
| TEST-ONBOARD-001 | The PT-BR tutorial run is completed by a fresh tester ([SPEC-ONBOARD](ONBOARDING.md)) | Not run |

QA may define additional localization tests (TEST-L10N-101 and up) in [TEST_SCENARIOS](../quality/TEST_SCENARIOS.md).

## 6. Open questions and risks

| ID | Summary |
|---|---|
| OPEN-026 | How CS2 code mods supply localized strings, and whether they can follow the game's language. |
| OPEN-015 | The UI technology, which affects text layout and font handling. |
| OPEN-303 (new) | Whether the mod can follow the game's units and display preferences. |
| OPEN-305 (new) | Who performs PT-BR review, and how the glossary aligns with the game's PT-BR terms. |
| OPEN-306 (new) | Wallet currency presentation. |
| RISK-303 (new) | PT-BR translation lag and text expansion breaking layouts or shipping untranslated strings. |

Full entries: [RISKS_AND_OPEN_QUESTIONS](../planning/RISKS_AND_OPEN_QUESTIONS.md).
