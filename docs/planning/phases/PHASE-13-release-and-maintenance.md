# PHASE-13 — Release and maintenance

| Field | Value |
|---|---|
| ID | PHASE-13 |
| Document status | Draft |
| Phase status | Not started |
| Milestone | M5 — Distributable v1.0 |
| Responsible role | Coordinator (release lead); QA (install, update, clean-exit tests); Reviewer (release review, license and package audit); independent tester; project owner (license confirmation, publication authorization, gate and M5 acceptance) |
| Related requirements | REQ-REL-001, REQ-REL-002, REQ-REL-003, REQ-REL-004, REQ-SAVE-006 (release candidate), REQ-COMPAT-001 (declaration) — see [PRD](../../product/PRD.md) |
| Last updated | 2026-10-18 |

## 1. Objective

Let someone else install and play without development tools. Freeze features and resolve blockers. Run a **release
review**, a **package, license, and provenance audit**, and tested **install, update, and clean-exit** procedures.
Publish a **compatibility declaration** and a **maintenance procedure**. An **independent tester** installs the mod on
a supported configuration and completes the intended experience. No safe-uninstall claim is made before the clean-exit
procedure has been tested.

## 2. Entry conditions

- PHASE-12 gate **Passed** (M4 reached), with no open critical defect.
- Project license confirmed by the owner (OPEN-027) after the [REUSE_AND_LICENSE_AUDIT](../../legal/REUSE_AND_LICENSE_AUDIT.md).
- Distribution platform requirements researched (OPEN-017).
- Publication remains unauthorized until the owner explicitly authorizes it at the end of this phase.

## 3. Dependencies

- **Phases:** PHASE-12.
- **Open questions:** OPEN-017 (platform requirements), OPEN-022 (clean exit, release-candidate pass), OPEN-027 (project
  license), OPEN-602 (independent tester), OPEN-023 (final conflict list).

## 4. Required reading

[PACKAGING_AND_PUBLISHING](../../release/PACKAGING_AND_PUBLISHING.md), [INSTALL_UPGRADE_UNINSTALL](../../release/INSTALL_UPGRADE_UNINSTALL.md),
[COMPATIBILITY_POLICY](../../release/COMPATIBILITY_POLICY.md), [POST_RELEASE_MAINTENANCE](../../release/POST_RELEASE_MAINTENANCE.md),
[REUSE_AND_LICENSE_AUDIT](../../legal/REUSE_AND_LICENSE_AUDIT.md), [ASSET_PROVENANCE](../../legal/ASSET_PROVENANCE.md),
[THIRD_PARTY_NOTICES](../../../THIRD_PARTY_NOTICES.md), [COMPATIBILITY_MATRIX](../../quality/COMPATIBILITY_MATRIX.md),
[KNOWN_ISSUES](../../quality/KNOWN_ISSUES.md), [QUICK_START](../../user/QUICK_START.md),
[TROUBLESHOOTING](../../user/TROUBLESHOOTING.md), [UPSTREAM_STRATEGY](../../engineering/UPSTREAM_STRATEGY.md),
[ADR-0001](../../architecture/decisions/ADR-0001-host-game-and-runtime.md), [CHANGELOG.md](../../../CHANGELOG.md).

## 5. Deliverables

- A release candidate built from a recorded commit, with its version and change log.
- A completed package audit: contents, dependencies, notices, provenance, and no proprietary game content.
- Tested install, update, and clean-exit procedures, recorded with evidence, on a configuration without development
  tools.
- The compatibility declaration (supported game version, DLCs, mods, known conflicts).
- Final user guides (quick start, troubleshooting, install/upgrade/uninstall) written from the tested procedures.
- The post-release maintenance procedure.
- The independent tester's acceptance run.
- The owner's release decision. Publication happens only after explicit owner authorization.

## 6. Proposed work packages

| WP | Title | Addresses | Notes |
|---|---|---|---|
| WP-13-01 | Feature freeze and blocker resolution | Gate | Only fixes from here on; each fix re-verified. |
| WP-13-02 | Package, license, and provenance audit | REQ-REL-003, OPEN-027, RISK-029 | Every incorporated component and asset; notices complete; license confirmed by the owner; no game binaries or extracted assets. |
| WP-13-03 | Packaging and dependency check | REQ-REL-004, OPEN-017, RISK-025 | Package per PACKAGING_AND_PUBLISHING; runtime needs only CS2 and documented dependencies, never Motor Town or Melty. |
| WP-13-04 | Install, update, and clean-exit tests | REQ-REL-001, REQ-REL-002, REQ-SAVE-006, OPEN-022 | On a configuration without SDKs or development tools: install, update from the previous build, clean exit (end control, park or hand back entities, save to a new copy, verify the save opens without the mod). |
| WP-13-05 | Compatibility declaration | REQ-COMPAT-001 | From COMPATIBILITY_MATRIX evidence only, per COMPATIBILITY_POLICY. |
| WP-13-06 | User documentation | REQ-REL-001 | Quick start, troubleshooting, install/upgrade/uninstall, known issues, credits, migration notes; CHANGELOG entry for the release. |
| WP-13-07 | Maintenance procedure | RISK-007 | Response to game updates, upstream review per UPSTREAM_STRATEGY, issue intake, compatibility re-testing. |
| WP-13-08 | Independent tester acceptance | OPEN-602 | A tester who did not build the mod installs it and completes the core experience without help. |
| WP-13-09 | Release review and owner decision | Gate | Reviewer release review; owner go or no-go; publication only with explicit authorization. |

## 7. Out of scope

Expansions listed in [EXPANSIONS](../../product/EXPANSIONS.md). Automated publishing pipelines unless the owner
authorizes them. Publication on more than one channel without checking each channel's terms (for example, any
exclusivity terms) with the owner. New features.

## 8. Tests

| Test | Layer (expected) | Scope in this phase | Validation |
|---|---|---|---|
| TEST-REL-001 | In-game | Install and run without development tools | Not run |
| TEST-REL-002 | In-game | Install, update, clean exit tested before any safe-uninstall claim | Not run |
| TEST-REL-003 | Review | Notices and audits complete for every incorporated component and asset | Not run |
| TEST-REL-004 | Review, In-game | Runtime needs only CS2 and documented dependencies | Not run |
| TEST-SAVE-006 | In-game | Clean-exit procedure on the release candidate | Not run |
| TEST-COMPAT-001 | Review | Compatibility declaration matches recorded evidence | Not run |
| TEST-REL-001 (independent acceptance) | In-game | Core experience completed by an independent tester; defined in [TEST_SCENARIOS](../../quality/TEST_SCENARIOS.md) | Not run |

## 9. Acceptance gate

Consistent with the [ROADMAP](../../../ROADMAP.md): *an independent tester installs the mod on a supported
configuration and completes the intended experience.* The [PRD](../../product/PRD.md) §9 success criteria apply in
full: every `Must` requirement Passed with evidence or covered by an owner-approved scope decision; the independent run
without developer tools or assistance; no known critical defects; notices, the license audit, and the compatibility
declaration complete. A Reviewer's release review and the owner's acceptance close the phase and milestone M5.

## 10. Risks and open questions

RISK-025 (distribution platform constraints), RISK-017 (unloadable saves after removal), RISK-029 (redistribution of
game content), RISK-015 (branding and naming review), RISK-002 (license obligations of incorporated code), RISK-007
(game updates after release), RISK-013 (claims without evidence). Open questions: section 3. All entries:
[RISKS_AND_OPEN_QUESTIONS](../RISKS_AND_OPEN_QUESTIONS.md).

## 11. Recovery

- **The clean-exit test fails.** Do not publish safe-uninstall instructions. Fix the cause and re-test, or document the
  exact limitation and get an owner decision before release.
- **The package audit finds missing notices or provenance.** Fix the notices or remove the item, then re-audit.
- **The independent tester cannot complete the experience.** Treat the blocking point as a defect, fix it, and repeat
  with a tester who has not seen the previous attempt.
- **Platform requirements conflict with the design** (RISK-025). Record a `CONF-*` entry and ask the owner for a scope
  decision; do not change runtime behavior silently to fit a platform.

## 12. Evidence record

None yet. Release-candidate runs will be recorded as `RUN-NNNN` under
[docs/quality/evidence/](../../quality/evidence/README.md). No release, package, or publication exists.

| Run | Date | Scope | Result |
|---|---|---|---|
| — | — | No runs yet | Not run |
