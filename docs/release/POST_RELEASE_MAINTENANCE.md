# Post-Release Maintenance

| Field | Value |
|---|---|
| ID | DOC-MAINTENANCE |
| Document status | Draft (pre-release draft) |
| Release scope | v1.0 and later maintenance releases |
| Phases | PHASE-13 (procedure defined and rehearsed), after release |
| Responsible role | Coordinator (triage and planning); Implementer (fixes); Reviewer; QA (verification); project owner (approves every publication and support decision) |
| Related requirements | REQ-COMPAT-001, REQ-SAVE-002, REQ-SAVE-004, REQ-DIAG-001, REQ-DIAG-002, REQ-REL-002 |
| Related documents | [COMPATIBILITY_POLICY](COMPATIBILITY_POLICY.md), [PACKAGING_AND_PUBLISHING](PACKAGING_AND_PUBLISHING.md), [TEST_STRATEGY](../quality/TEST_STRATEGY.md), [KNOWN_ISSUES](../quality/KNOWN_ISSUES.md), [SPEC-SAVE](../architecture/SAVE_FORMAT.md), [UPSTREAM_STRATEGY](../engineering/UPSTREAM_STRATEGY.md), [SECURITY.md](../../SECURITY.md) |
| Last updated | 2026-10-18 |

> **Pre-release draft.** Nothing has been released, so nothing is being maintained. **No support channel exists**, and
> none is named here until the owner decides one ([OPEN-702](../planning/RISKS_AND_OPEN_QUESTIONS.md)).

## 1. Purpose

Defines how the project will respond after v1.0 to game updates, defects, and changes in dependencies, and how
features and save schemas are retired without harming players' cities.

## 2. Game-update response

1. **Trigger:** a game update, hotfix, or DLC release becomes known (RISK-007).
2. **Mark:** the new version is Untested in [COMPATIBILITY_MATRIX](../quality/COMPATIBILITY_MATRIX.md). No statement
   about it is made until runs exist.
3. **Prepare:** with owner authorization, update the lab environment after any running campaign ends; re-verify the
   toolchain in [TOOLCHAIN_MATRIX](../engineering/TOOLCHAIN_MATRIX.md).
4. **Test:** run the smoke suite (TEST-COMPAT-103, [TEST_STRATEGY](../quality/TEST_STRATEGY.md) §11), then the
   compatibility test set ([COMPATIBILITY_POLICY](COMPATIBILITY_POLICY.md) §4); re-measure the benchmark baseline.
5. **Decide:**
   - Passed → add the Supported row and update the declaration.
   - Failed → record `ISSUE-*` entries; publish a notice through the support channel advising players to keep a save
     copy and use the safe-disable switch or keep the mod disabled (REQ-DIAG-002); start a hotfix (section 3).

## 3. Hotfix process [Proposal]

A hotfix is a PATCH release that fixes a Critical or High defect without new features.

1. Reproduce the defect in a lab city; record a `RUN-*` and an `ISSUE-*` entry.
2. Create a task for the fix; once Git exists, work on a dedicated hotfix branch ([CONTRIBUTING.md](../../CONTRIBUTING.md)).
3. Make the smallest change that fixes the cause. A critical-area fix needs a scoped independent review
   ([ROLES_AND_OWNERSHIP](../agents/ROLES_AND_OWNERSHIP.md) §5).
4. Run the failing test, the critical-area suite, and the save-safety subset. **No hotfix skips the save-safety tests.**
5. Bump the version, write release notes, and verify the package ([PACKAGING_AND_PUBLISHING](PACKAGING_AND_PUBLISHING.md) §9).
6. The owner authorizes publication. After publication, install from the channel on a clean configuration and repeat
   the smoke checks.
7. Update [KNOWN_ISSUES](../quality/KNOWN_ISSUES.md), [CHANGELOG.md](../../CHANGELOG.md), the matrix, and
   [PROJECT_STATUS](../PROJECT_STATUS.md).

## 4. Severity levels [Proposal]

| Severity | Examples | Response |
|---|---|---|
| Critical | Save corruption; career data leaking between save revisions; duplicated rewards; crash on load; city changed by the mod | Notice as soon as confirmed; hotfix before any other work |
| High | Permanent loss of control; mission or vehicle softlock with no recovery; broken after a game update | Hotfix |
| Medium | Recoverable malfunction with a workaround; wrong feedback | Next planned release |
| Low | Cosmetic or wording issue | Next planned release |

## 5. Deprecation

- **Features:** announced in release notes at least one release before removal, with a migration path for any saved
  data that depends on them. Removing a v1.0 requirement needs the owner's scope decision ([PRD](../product/PRD.md) §11).
- **Game versions:** dropping support is stated in the compatibility declaration; earlier evidence stays in the matrix.
- **Save schemas:** older schemas migrate forward. Support for migrating a very old schema is removed only through an
  owner decision and release-note notice. Data that can no longer be migrated is refused without loss (REQ-SAVE-002).
- **End of maintenance:** if the project stops, a final notice states the last tested combination and the clean-exit
  procedure ([INSTALL_UPGRADE_UNINSTALL](INSTALL_UPGRADE_UNINSTALL.md) §5).

## 6. Migration rules

- Migrations are forward-only, versioned by the schema version, and idempotent.
- Every released schema version has a lab fixture: a save or sidecar created by that release in a lab city, stored
  without personal data, used to test each new migration ([SAVE_SAFETY_TESTS](../quality/SAVE_SAFETY_TESTS.md)).
- A failed migration quarantines the data and keeps the last valid copy (REQ-SAVE-004).
- A migration that changes ledger or wallet data is a critical-area change (persistence) and needs scoped review.

## 7. Support channels (to be decided)

The owner decides where players report problems and where notices are posted, including a **private** route for
security problems ([OPEN-702](../planning/RISKS_AND_OPEN_QUESTIONS.md), [SECURITY.md](../../SECURITY.md) §6). Until
then, no channel is named in any document, and [TROUBLESHOOTING](../user/TROUBLESHOOTING.md) lists only what a report
must contain. A useful report includes the mod version and build ID, the game version, DLCs, other mods, steps, and a
sanitized diagnostics bundle (REQ-DIAG-001).

## 8. Upstream and dependency monitoring

- Changes in the Beta Test Driving Mod upstream, if any of its code is incorporated, are reviewed per
  [UPSTREAM_STRATEGY](../engineering/UPSTREAM_STRATEGY.md) before adoption; the pinned revision changes only through a
  task, a license re-check, and the regression suite.
- Dependency updates are pinned and recorded in [DEPENDENCIES](../engineering/DEPENDENCIES.md); security-relevant
  updates are prioritized ([SECURITY.md](../../SECURITY.md) §4).

## 9. Open questions and risks

OPEN-017 (platform update flow), [OPEN-701](../planning/RISKS_AND_OPEN_QUESTIONS.md) (channel choice),
[OPEN-702](../planning/RISKS_AND_OPEN_QUESTIONS.md) (support and security-report channels, new), OPEN-508 (holding the
game version constant); RISK-005, RISK-006, RISK-007, RISK-016, RISK-017.
