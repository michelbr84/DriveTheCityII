# Quick Start

| Field | Value |
|---|---|
| ID | DOC-QUICK-START |
| Document status | Draft (pre-release draft) |
| Release scope | v1.0 |
| Phases | PHASE-06 (first complete loop), PHASE-11 (tutorial and final wording), PHASE-13 (published guide) |
| Responsible role | Coordinator (maintainer); QA verifies each step on a release candidate |
| Related requirements | REQ-ONBOARD-001, REQ-REL-001, REQ-REL-004, REQ-TAXI-001, REQ-GARAGE-001, REQ-CTRL-004, REQ-TIME-001, REQ-ECON-001, REQ-SAVE-001, REQ-COMPAT-001 |
| Related documents | [PLAYER_JOURNEYS](../product/PLAYER_JOURNEYS.md), [SPEC-ONBOARD](../experience/ONBOARDING.md), [SPEC-INPUT](../experience/CONTROLS_AND_ACCESSIBILITY.md), [SPEC-TAXI](../gameplay/careers/TAXI.md), [INSTALL_UPGRADE_UNINSTALL](../release/INSTALL_UPGRADE_UNINSTALL.md), [TROUBLESHOOTING](TROUBLESHOOTING.md) |
| Last updated | 2026-10-18 |

> **STATUS: NOT YET INSTALLABLE.**
>
> Drive the City II is in **pre-development**. There is **no download, no package, and no build**, and nothing below
> has been implemented or tested. This page describes the **intended** first-play flow so that design, tutorial, and
> release work share one target. Do not look for a download; none exists.

## 1. Who this is for

Eventually: players starting Drive the City II for the first time. Today: designers, testers, and agents who need the
intended first session in one place. The final version is rewritten with the real interface text in PHASE-11 and
verified by an independent tester in PHASE-13.

## 2. Before you start (intended)

- A Cities: Skylines II version declared supported in [COMPATIBILITY_POLICY](../release/COMPATIBILITY_POLICY.md), on
  Windows.
- Only the mod and its declared dependencies. **Motor Town and Melty are not needed** (REQ-REL-004), and no
  development tools are needed (REQ-REL-001).
- The original Beta Test Driving Mod must **not** be active at the same time (REQ-COMPAT-001).
- Make a copy of the city save you plan to use. Try the mod on the copy first.

## 3. Your first session (intended flow)

1. **Install and enable** the mod as described in [INSTALL_UPGRADE_UNINSTALL](../release/INSTALL_UPGRADE_UNINSTALL.md).
2. **Load your city copy.** You start in Building mode, as usual.
3. **Enter character mode** with the mod's action for it. Your avatar appears at a validated safe position. The
   binding is not final: proposed bindings are in [SPEC-INPUT](../experience/CONTROLS_AND_ACCESSIBILITY.md), and all
   actions will be remappable (REQ-INPUT-001).
4. **Follow the tutorial** to a rental service point. A basic rental is always available, even with little money
   (REQ-GARAGE-001).
5. **Rent a car and get in.** Walk up to the stationary vehicle and use the interaction prompt (REQ-TRANS-001).
6. **Accept your first taxi fare.** You see an estimate before accepting (REQ-TAXI-003).
7. **Drive to the pickup point** at the curb. A career passenger boards. Drive to the drop-off point; the route is shown
   on the city's roads (REQ-UI-004).
8. **Get paid once.** The fare goes to your personal wallet and appears in your transaction history (REQ-ECON-002).
9. **Park and get out.** The mod places your avatar at a safe spot beside the vehicle (REQ-TRANS-002).
10. **Return to Building mode** whenever you like. The simulation speed you had before is restored (REQ-TIME-001,
    REQ-CTRL-004).
11. **Save to a new save, quit, and reopen it.** Your wallet, vehicle, and progress continue from where you left them
    (REQ-TAXI-001, REQ-SAVE-001).

## 4. Good to know (intended design)

- Your **wallet** is your own career money. It is separate from the city budget, and career work does not change city
  finances (REQ-ECON-001).
- **Career passengers and cargo** belong to the mod; they are not your city's citizens or goods (REQ-TAXI-002).
- **Companies** you found later are a managerial simulation: their employees and fleet are calculated, not driving
  around your city (REQ-COMPANY-002).
- **Editing the city** can make a job impossible. The mod then reroutes it or cancels it without penalty
  (REQ-CAREER-006).
- **If you get stuck**, use Recovery, or return to Building mode ([TROUBLESHOOTING](TROUBLESHOOTING.md)).
- The interface is planned in **English and Brazilian Portuguese (PT-BR)** (REQ-L10N-001), with gamepad support
  (REQ-INPUT-002).

## 5. Verification before this guide is published (validation: Not run)

- [ ] Every step above performed by a new player without developer help on a release candidate (TEST-ONBOARD-001).
- [ ] Every step performed on a clean configuration without development tools (TEST-REL-001).
- [ ] Wording, names of actions, and bindings replaced with the text actually shown in the game.
- [ ] The "Not yet installable" banner removed only after the owner approves publication.
