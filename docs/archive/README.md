# Archive

| Field | Value |
|---|---|
| ID | DOC-ARCHIVE |
| Document status | Draft |
| Scope | Superseded documents and decisions whose rationale must be preserved |
| Responsible role | Coordinator (maintainer); project owner approves archiving of Accepted documents and ADRs |
| Related documents | [DOCUMENTATION_RULES](../DOCUMENTATION_RULES.md), [INDEX](../INDEX.md), [ADR index](../architecture/decisions/README.md), [READING_GUIDE](../agents/READING_GUIDE.md) |
| Last updated | 2026-10-18 |

> **Nothing has been archived** as of 2026-10-18. This folder holds only this policy.

## 1. Purpose

The archive keeps superseded material whose reasoning someone may need later, out of the default reading path, so
that agents never mistake an old decision for current guidance.

## 2. When to archive

Archive a document when **all** of these hold:

- it is **superseded**: a replacement document or ADR now owns its content, or the decision was reversed;
- its rationale, evidence, or rejected options are worth preserving (for example an ADR replaced after an experiment,
  or a specification restructured into several new ones);
- it is no longer active: nothing should be implemented or tested against it.

Do **not** archive:

- ordinary revisions, corrections, or rewrites of a document that keeps its role: version control (once it exists,
  OPEN-029) and [CHANGELOG.md](../../CHANGELOG.md) cover those, and the archive is not a revision history;
- Proposed or Rejected ADRs that still explain the current design: they stay in the
  [decisions](../architecture/decisions/README.md) folder with their status;
- historical inputs that the owner required to be preserved unchanged (see section 5).

## 3. How to archive

1. **Mark it.** Set the document status to Superseded (ADR status Superseded) and add a line directly under the
   metadata table that starts with "Superseded by", links the replacement by its ID, and gives the date of archiving
   and a one-line reason.
2. **Update the replacement.** Its "Related documents" names the archived file as superseded material.
3. **Move it** into `docs/archive/`, keeping its original file name. If the name is already used, prefix the date of
   archiving (`YYYY-MM-DD-`).
4. **Fix links.** Repair or remove the archived file's own relative links so they resolve from the new location, and
   point every inbound link in active documents to the replacement (or, where the history itself is the subject, to
   the archived file).
5. **Update registers in the same change:** [INDEX](../INDEX.md) (remove from the active lists),
   the [ADR index](../architecture/decisions/README.md) (keep the row, status Superseded, link to the archived file and
   to the replacement), [TRACEABILITY](../planning/TRACEABILITY.md) if requirements pointed to it, the
   [register](../planning/RISKS_AND_OPEN_QUESTIONS.md) entries that cite it, and [CHANGELOG.md](../../CHANGELOG.md).
6. **Record it** in the table in section 4.

IDs of archived documents are never reused. Archived files are not edited afterwards, except for the supersession line
and link repairs.

## 4. Archive register

| Archived file | Original path | Archived on | Superseded by | Reason |
|---|---|---|---|---|
| None | — | — | — | Nothing has been archived. |

## 5. Historical inputs are not archived here

[chat.md](../../chat.md), the Portuguese export of the earlier planning conversation (SRC-006), stays **unchanged at
the project root**. It is not moved into this folder because the owner required original source documents to be
preserved as they are. Like the archive, it is excluded from the default agent reading path
([READING_GUIDE](../agents/READING_GUIDE.md) §7) and is read only for provenance, with every statement treated as
**[Historical input]**.

## 6. Reading archived material

Archived documents are history, never instructions. A task that needs one names it in its "Required reading" and states
why. When an archived document and an active one disagree, the active one wins.
