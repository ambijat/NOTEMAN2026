# NoteMan Direction of Refinement

Companion to `ONTOLOGICAL_BASIS.md` (the entities) and `ATTRIBUTES.md` (their attributes). This document records what the NOTEMAN2026 workspace inherits from each prior generation and the direction in which the inheritance is being refined.

## 1. The Inheritance Chain

Four generations, each contributing a permanent idea:

| Generation | Artifact | Permanent contribution |
|---|---|---|
| Visual Basic | `noteman1.txt` | Notes are files in a chosen folder; browse, compare, and search belong in the same tool as capture |
| Python Tkinter | `nsu6.py` … `nsu64.py`, `nsu62b.py` | The fast capture loop: folder → timestamped note → paste with `Reference{Page}` → export → reset; OCR as source conversion |
| C# translation | `archive/legacy-noteman/nsu62.cs` | The workflow is portable — not bound to any toolkit, language, or OS |
| WCS reinvention | `apps/ubuntu-python`, `apps/windows-dotnet` | The referenced fragment replaces the file as the central object; capture becomes a research-ethics discipline |

The direction of refinement is the continuation of one long movement:

```text
text box with export buttons
  -> capture tool with OCR
    -> portable workflow
      -> fragment-centric research capture system
        -> compartmentalized research-ethics instrument
```

Each refinement keeps the previous layer working. Nothing inherited is discarded; it is restated at a higher level of structure.

## 2. Refinement Axes

Every planned change falls on one of five axes. A change that does not advance an axis — or that violates the constraints column — should be redesigned.

| Axis | From (inherited state) | Toward (refined state) | Constraint |
|---|---|---|---|
| **Object model** | Text in a buffer, exported to a file | Typed fragments with source, locator, method, asset | Fragment invariant: text or recoverable asset, always |
| **Provenance** | `Reference{Page}` naming convention | Typed Source + Locator on every fragment, OCR confidence next | Extraction must never strip provenance |
| **Process visibility** | Hidden global state, silent resets | Explicit state machine, visible compartments, deliberate transitions | No silent loss; no invisible automation |
| **AI assistance** | None | Prompt workbench → clipboard → draft → review → accept | AI output stays draft until human review; nothing auto-sent |
| **Retrieval** | VB browse/compare/search (lost in the Python era) | Fragment-level search, side-by-side review, bibliography | Search over fragments, not only raw files |

## 3. Governing Discipline

The refinement is constrained by the Coding Testament (`apps/ubuntu-python/docs/CODING_TESTAMENT.md`, mirrored in `apps/windows-dotnet`). Its operative test for every feature:

```text
Does this preserve compartments, source trail, and deliberate user action?
```

Key disciplines that shape what "refinement" is allowed to mean here:

- **Brakes before automation.** Automated actions need visible boundaries, recoverable state, and user-controlled acceptance before they need speed.
- **Clipboard as the primary bridge.** Copy-paste is inspectable and model-independent; it is preferred over API integration precisely because it keeps the researcher in the loop.
- **No vendor lock-in.** The system must work with any tool that accepts clipboard or local text — ChatGPT, Claude, Ollama, or none.
- **The researcher is the checker.** The system drafts; it never grades its own output as final research material.

This is why the refinement direction is deliberately conservative: NoteMan refines toward *more structure and more visibility*, not toward more automation.

## 4. Current Position on the Roadmap

The five-phase plan from `ONTOLOGICAL_BASIS.md` Section 8, with live status:

### Phase 1 — Stabilize current behavior: **superseded**

Rather than refactoring `nsu62b.py` in place, the WCS rewrite absorbed its lessons (state flags, OCR ordering, cross-platform paths) into a clean package. The legacy scripts are frozen in `archive/legacy-noteman` under a preservation policy: historical behavior is retained, no new features.

### Phase 2 — Introduce domain models: **done**

`domain.py` implements Workspace, Project, Note, Source, Locator, CaptureFragment, and Asset with the fragment invariant enforced in code. `storage.py` implements the file repository with JSON sidecars and Markdown export, per the shared contract in `WORKSPACE_FORMAT.md`.

### Interphase — AI compartment (not in the original plan): **done**

The prompt workbench, `ai_corpus/` storage compartment, and `ai_draft` extraction method were added between Phases 2 and 3. This is the largest genuinely new inheritance the 2026 workspace passes forward.

### Current objective — Ubuntu Ollama screenshot reader: **in progress**

Defined in `apps/ubuntu-python/NEXT_OBJECTIVE.md`:

```text
screenshot/image -> local OCR or Ollama vision -> cleaned text/summary
  -> CaptureFragment -> Markdown + JSON note
```

Safety rule: screenshots are sensitive; local tools only (Tesseract for exact OCR, Ollama vision for cleanup) — no cloud APIs by default. Done when one local image can be processed end-to-end into the shared workspace format with tests passing on Ubuntu and no Windows-only dependency.

### Phase 3 — Restore knowledge management: **not started**

Bring back what the VB version already had, at fragment level: search across all notes in a project, side-by-side comparison, review as a first-class mode rather than "open in external editor."

### Phase 4 — Improve source fidelity: **partially started**

Assets are now preserved (copied, never deleted) — the reversal of the legacy delete-after-OCR behavior is done. Still open: OCR confidence on fragments, page ranges, richer source types, deduplication of repeated captures.

### Phase 5 — Modernize interface: **ongoing on two tracks**

The Ubuntu Tkinter shell (`apps/ubuntu-python`) and the Windows WPF shell (`apps/windows-dotnet`) evolve in parallel. The goal is identical research behavior, not identical code; each completed platform session leaves a parity handoff (`docs/PLATFORM_PARITY_HANDOFF_TEMPLATE.md`) so the other platform can emulate it.

## 5. Repository Ecosystem Direction

From `apps/ubuntu-python/docs/REPOSITORY_STRATEGY.md` and the READMEs:

| Repository | Role | Direction |
|---|---|---|
| `NOTEMAN` | Legacy archive | Frozen; preserve distinct historical capabilities, accept no major features |
| `apps/ubuntu-python` | Core ontology, storage, Python reference | Primary home for Ubuntu/Linux work and shared-format decisions |
| `apps/windows-dotnet` | Windows C#/WPF shell | Primary home for Windows work; parity via the shared workspace format |
| `NOTEMAN-OCR` | Future extraction adapters | Create only when implementation justifies it |
| `NOTEMAN-RESEARCH-KIT` | Future templates and student workflows | Create only when implementation justifies it |

Rule of placement: the platform chooses the principal implementation — Linux work lands first in `apps/ubuntu-python`, Windows work first in `apps/windows-dotnet`, and the workspace format is the treaty between them.

## 6. Near-Term Refinement Queue

Ordered by dependency, derived from the gaps recorded in `ATTRIBUTES.md`:

1. **Finish the screenshot reader** (current objective) — it exercises Asset, Extraction, and the storage contract end-to-end.
2. **Store OCR confidence and cleanup status** on fragments — cheap to add while the extraction pipeline is being built, expensive to retrofit later.
3. **Fragment-level search** (opens Phase 3) — the first Review feature; establishes that retrieval operates on fragments, not files.
4. **Export audit trail** — record when and where a note was exported, making the export invariant ("prevent accidental duplicate commits") checkable.
5. **Side-by-side review** — restores the last major VB capability still missing.
6. **Deduplication of repeated captures** — needs search infrastructure first, hence its position.

## 7. The Test of Any Future Refinement

A change belongs in this project's lineage if it can answer yes to all four:

1. Does it strengthen the fragment as the central object?
2. Does it preserve or enrich the source trail?
3. Does it keep every transition visible and reversible until the researcher accepts it?
4. Does it work without any particular vendor, model, toolkit, or OS?

A change that answers no to any of these is not a refinement of NoteMan — it is a different project.
