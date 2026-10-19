# Known Issues

| Field | Value |
|---|---|
| ID | DOC-KNOWN-ISSUES |
| Document status | Draft |
| Release scope | v1.0 and every later release |
| Phases | All phases with runtime work (PHASE-00 onwards) |
| Responsible role | QA (maintainer: records observed defects); Implementer (fixes); Reviewer (verifies fixes from evidence); project owner (approves "Won't fix" for Critical or High issues) |
| Related requirements | All `REQ-*` in the [PRD](../product/PRD.md); the PRD §9 release criterion "no known critical defects" |
| Related documents | [TEST_STRATEGY](TEST_STRATEGY.md), [evidence](evidence/README.md), [RISKS_AND_OPEN_QUESTIONS](../planning/RISKS_AND_OPEN_QUESTIONS.md), [BASE_MOD_AUDIT](../research/BASE_MOD_AUDIT.md), [TROUBLESHOOTING](../user/TROUBLESHOOTING.md), [POST_RELEASE_MAINTENANCE](../release/POST_RELEASE_MAINTENANCE.md) |
| Last updated | 2026-10-18 |

## 1. Current state

> **No runtime-defect assessment exists.** As of 2026-10-18 nothing has been built, installed, or run, so no defect can
> have been observed. This list has **zero entries**. The empty list does not mean the mod is free of defects; it means
> no testing has happened yet.

## 2. What belongs here and what does not

| Situation | Where it goes |
|---|---|
| A defect **observed** in our mod during a recorded run or reported with reproducible steps | Here, as `ISSUE-NNN` |
| A feature that is not implemented yet | Not a defect. The related test stays **Not run**; the work is in the [ROADMAP](../../ROADMAP.md), phase documents, and [BACKLOG](../planning/BACKLOG.md). |
| An anticipated problem that has not been observed (for example save corruption as a possibility) | The [risk register](../planning/RISKS_AND_OPEN_QUESTIONS.md) as `RISK-*` |
| An unanswered technical question | The register as `OPEN-*` |
| A contradiction between documents | The register as `CONF-*`, or the documentation validation report for documentation-only checks |
| Behavior observed in the **candidate base mod** during baseline validation (TASK-0003), before any of its code is incorporated | [BASE_MOD_AUDIT](../research/BASE_MOD_AUDIT.md) findings. It becomes an `ISSUE-NNN` only if the code is incorporated and the defect persists in our mod. |
| Third-party reports about other projects (for example issues filed upstream) | External claims recorded in [SOURCES](../research/SOURCES.md) or the relevant audit; never here unless we reproduce them in our mod |
| A host-game problem that affects our mod | Here, labeled "host game", with the game version, if it affects players of our mod; otherwise a note in the run |

## 3. Severity scale

| Severity | Definition | Examples |
|---|---|---|
| **Critical** | Breaks a release-blocking guarantee. Any open Critical issue blocks the PHASE-12 and PHASE-13 gates ([PRD](../product/PRD.md) §9). | Save corruption or loss of career data; money, progress, or vehicles leaking between save revisions; duplicated rewards or money; permanent loss of control (no way back to `Building` or `OnFoot` without restarting the game); mandatory content that cannot be completed; an unintended persistent change to the city; a city that no longer loads after the mod is disabled or removed. |
| **High** | Severe malfunction with a workaround, or a crash without data loss. | Game crash that loses only unsaved progress; recurring `Recovery` loops; a career that can only be continued through a documented workaround; a handback that leaves a temporary traffic blocker. |
| **Medium** | A feature misbehaves, but the player can continue normally. | Wrong HUD value; an exit point on the less preferred side; a missing PT-BR string; inconsistent challenge scoring. |
| **Low** | Cosmetic or minor. | Small camera jitter; a typo; a placeholder label in the wrong place. |

Severity is assigned by QA and confirmed by the Reviewer. Lowering the severity of a Critical issue requires the
project owner's approval and a recorded reason.

## 4. Issue status values

`Open` → `Investigating` → `Fixed – awaiting verification` → `Verified fixed` (with the verifying `RUN-NNNN`).
Other terminal values: `Duplicate` (with a link to the original), `Not reproducible` (after a documented attempt), and
`Won't fix` (requires a recorded reason; for Critical or High issues, the project owner's approval). A fix is never
marked verified because it compiles.

## 5. Entry format

Copy this block for each new issue. Number issues sequentially (`ISSUE-001`, `ISSUE-002`, …); never reuse a number.

```markdown
### ISSUE-NNN — <short, specific title>
- Severity: Critical / High / Medium / Low
- Status: Open
- Component: <internal module (CityBridge, Player, WorldContact, Vehicles, Career, Persistence, UI, Diagnostics) or "host game">
- First observed: RUN-NNNN on YYYY-MM-DD (or a reproducible report with its source)
- Environment: game version · store edition · DLCs · mod version/commit · other mods (see COMPATIBILITY_MATRIX)
- Affected requirements and tests: REQ-… / TEST-…
- Steps to reproduce: 1) … 2) … 3) …
- Expected: …
- Observed: …
- Frequency: always / intermittent (n of m attempts) / once
- Workaround: … (or "none known")
- Fix: task or change reference (once it exists)
- Verification: RUN-NNNN (once it exists)
- Notes: sanitized log excerpt references; no personal data
```

## 6. Issues

There are no entries. No defect has been observed because nothing has been run.

## 7. Release use

- Before each phase gate, QA lists the open issues by severity in the gate evidence.
- The PHASE-12 and PHASE-13 gates require **zero open Critical issues**.
- Player-facing workarounds for open issues are summarized in [TROUBLESHOOTING](../user/TROUBLESHOOTING.md) once a
  release exists.
