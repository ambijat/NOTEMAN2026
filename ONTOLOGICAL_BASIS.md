# NoteMan Ontological Basis

This document extracts the durable concepts behind the Visual Basic and Python NoteMan/NoteMaker variants in this folder. Its purpose is to guide the next evolution of the project without tying the design to any one GUI toolkit, operating system, or file layout.

This edition is upgraded to reflect the realized successor implementation in `apps/ubuntu-python` (Python reference) and `apps/windows-dotnet` (Windows C#). The ontology below is no longer only a proposal: most entities now exist in code (`apps/ubuntu-python/src/noteman_wcs/domain.py`) and in a shared storage contract (`apps/ubuntu-python/docs/WORKSPACE_FORMAT.md`). Section 10 records what is realized and what remains open.

Companion documents carry the enhancement detail:

- `ATTRIBUTES.md`: the attribute catalog for every entity, verified against the code
- `DIRECTION_OF_REFINEMENT.md`: the inheritance chain and where refinement is heading

## 1. Reading Of Existing Versions

### Visual Basic version: `noteman1.txt`

The Visual Basic GUI establishes the first ontology of the application:

- A note is a plain text file created inside a chosen destination folder.
- A session has a working text area where clipboard material is accumulated before export.
- A folder can be browsed for existing notes.
- Two existing notes can be loaded into separate text panes for comparison or parallel reading.
- A search location and search query can return matching text files.

This version is not only a note writer. It also contains early knowledge-management operations: browse, inspect, compare, and search.

### Python Tkinter line: `nsu6.py` to `nsu64.py`

The Python versions narrow the interface into a capture-and-export tool, then extend it with OCR:

- `nsu6.py` introduces the central capture flow: choose/create working folder, create timestamped note file, paste clipboard text with reference and page number, export to text, reset.
- `nsu62.py`, `nsu62_a.py`, `nsu63w.py`, and `nsu64.py` vary operating-system assumptions, external editor choice, and image-folder behavior.
- `nsu62w.py` and `nsu63w.py` add Windows-specific Tesseract location selection.
- `nsu63w.py` adds clipboard-image OCR.
- `nsu62b.py` is the most advanced branch: natural image sorting, clipboard-image OCR for Linux clipboard tools, single-file OCR selection, page decrement, preview of clipboard image, and state flags that enforce export/reset order after clipboard OCR.

The Python line is centered on source extraction: clipboard text, image files, and clipboard images are converted into structured text fragments.

## 2. Core Ontology

The project should be understood as a system for transforming study/research sources into durable, referenced notes.

### Primary Entities

**Workspace**

The top-level selected location where one or more projects live.

Current code expression:

- VB: `fp`, `kp`, `u`
- Python: folder selected through `filedialog.askdirectory`

Future role:

- Owns projects, search indexes, configuration, and export policies.

**Project / Note Folder**

A named folder created by the user for a topic, book, lecture, paper, class, case, or research task.

Current code expression:

- `tb6`: "Folder Name Here"
- `foloc`: actual folder path

Future role:

- The natural unit of organization.
- Contains notes, source images, imported files, metadata, and derived OCR text.

**Note**

A durable text artifact, usually timestamp-named, containing captured fragments.

Current code expression:

- `tb3`: note filename
- `noteYYMMDDHHMM.txt`
- VB: `newDDMMMyyyyhhmm.txt`

Future role:

- A structured document with title, creation time, body, references, fragments, and export history.

**Capture Buffer**

The temporary text editor area where material accumulates before being exported.

Current code expression:

- Python: `tb1`
- VB: `tb1`

Future role:

- A draft transaction. It should know whether it has unsaved content, exported content, or OCR content awaiting confirmation.

**Source**

The origin of a captured fragment.

Current code expression:

- `tb4`: "Reference..."
- implicit clipboard, image file, or OCR image source

Future role:

- A first-class entity: book, PDF, webpage, article, lecture, image, screenshot, clipboard text, handwritten page, or unknown source.

**Locator**

A position inside a source.

Current code expression:

- `tb5`: page number
- formatted as `Reference{Pgs.}`

Future role:

- Page number, page range, timestamp, section heading, URL fragment, image filename, bounding box, or document coordinate.

**Capture Fragment**

One atomic captured unit: a source reference plus extracted text.

Current code expression:

```text
Reference{Page}
captured text

```

Future role:

- The core unit of note-making.
- Should carry text, source, locator, capture method, timestamp, confidence, and cleanup status.

**Asset**

A non-text input or companion file.

Current code expression:

- `.png`, `.jpg` files in `foloc` or `foloc/imgdata`
- clipboard image

Future role:

- Stored screenshot, scan, image, PDF page, audio segment, or external file.

**Extraction**

The process that turns an input into note text.

Current code expression:

- `paste()`: clipboard text extraction
- `imgps()`: OCR batch extraction
- `clpocr()`: OCR from clipboard image
- `ocr_read()`: OCR from selected image

Future role:

- A pluggable pipeline with different extractors: clipboard, Tesseract OCR, PDF text, image OCR, speech-to-text, browser clipping, manual typing.

**PromptTemplate**

A local, plain-text, editable instruction that renders a captured fragment into an AI prompt while preserving source and locator context. Added by the WCS generation; it did not exist in the VB or legacy Python versions.

Current code expression:

- `apps/ubuntu-python/src/noteman_wcs/prompts.py` and the editable corpus in `src/noteman_wcs/prompts/`

Future role:

- Keeps AI assistance inside visible compartments: the prompt body is always inspectable text, never a hidden instruction. Prompt groups classify tasks for selection but must not change source, draft, note, or export state.

**Export**

The act of committing buffer content to durable storage.

Current code expression:

- `export()`: append/write text to note file

Future role:

- A commit operation with validation, append policy, conflict policy, file format, and audit trail.

**Review / Retrieval**

The act of finding, opening, comparing, or searching existing notes.

Current code expression:

- VB browse folder/file comboboxes
- VB dual text panes `tb3`, `tb4`
- VB search using `FindInFiles`
- Python `opennote()`

Future role:

- Search, compare, tag, filter, link, preview, and review captured knowledge.

## 3. Relationships

The following relationships should become explicit in the next architecture:

```text
Workspace contains Project
Project contains Note
Project contains Asset
Note contains CaptureFragment
CaptureFragment cites Source
CaptureFragment locates Locator
CaptureFragment may deriveFrom Asset
Extraction produces CaptureFragment
PromptTemplate renders CaptureFragment into AI prompt
Export commits CaptureBuffer into Note
Review queries Note and CaptureFragment
```

In simpler terms:

```text
Source -> Extraction -> Fragment -> Buffer -> Export -> Note -> Review
```

This is the conceptual spine of NoteMan.

## 4. State Model

The current scripts encode state through global variables and text-box contents. The ontology should separate user interface state from domain state.

### Current State Signals

- Folder selected: `foloc` exists.
- Project name entered: `tb6` is not placeholder text.
- Note selected/created: `tb3` contains a filename.
- Buffer has content: `tb1` contains text.
- Export completed: buffer begins with "Content Exported to".
- OCR tool configured: `tessloc != 'not_set'`.
- Clipboard OCR must be exported/reset: `needs_export`, `needs_reset` in `nsu62b.py`.

### Future State Machine

```text
NoWorkspace
  -> WorkspaceSelected
  -> ProjectReady
  -> NoteReady
  -> BufferDirty
  -> Exported
  -> Reviewed
```

Additional substates:

- `ExtractorUnavailable`
- `ExtractionPending`
- `ExtractionFailed`
- `UnsavedChanges`
- `ExportBlocked`
- `ReviewMode`

The key invariant is: extracted or pasted material should never be silently lost.

## 5. Domain Invariants

These rules should survive any future rewrite:

- A capture fragment must have text or a recoverable asset reference.
- A note must belong to one project.
- A project must have a filesystem location or a storage identity.
- Export should be idempotent enough to prevent accidental duplicate commits.
- Reset should warn when the buffer contains unexported content.
- OCR output should preserve its source and locator.
- Page navigation should operate on a real locator, not just a loose text box.
- Search should operate over notes and fragments, not only raw files.
- External editor launch should be optional; the app itself should remain able to read notes.
- AI-generated text remains draft material until explicitly reviewed by the researcher.
- Source material must never be auto-sent to an AI; text leaves the app only through a deliberate user action (clipboard by preference).
- Automated actions must have visible boundaries, recoverable state, and user-controlled final acceptance.
- The system preserves researcher comprehension; it never replaces it with hidden automation.

### Compartment Model

Inherited from `docs/CODING_TESTAMENT.md` in both active repositories: material of different epistemic status must never mix silently. The five compartments are:

```text
Source text          (captured, provenance attached)
AI prompt            (visible, locally editable)
AI result            (draft until reviewed)
Human-reviewed note  (accepted by the researcher)
Exported note        (durable, intentional)
```

Storage mirrors these compartments: `assets/` holds source inputs, `ai_corpus/` holds reviewed AI drafts, `notes/` holds accepted exports. Movement between compartments is always a deliberate user action.

## 6. Architectural Consequences

The current programs mix GUI widgets, domain state, filesystem logic, OCR logic, and validation in single files. The next evolution should separate these layers:

```text
noteman/
  domain/
    models.py        # Workspace, Project, Note, Source, Locator, Fragment, Asset
    services.py      # capture, export, search, review workflows
  storage/
    filesystem.py    # folders, note files, assets, metadata
    formats.py       # txt, markdown, json sidecar
  extractors/
    clipboard.py
    tesseract.py
    image_files.py
  ui/
    tkinter_app.py   # or another GUI layer
  config.py
```

This keeps the ontology independent from Tkinter, Visual Basic, Notepad, Leafpad, GNOME Text Editor, Windows clipboard APIs, or Linux clipboard commands.

This separation has since been realized in `apps/ubuntu-python/src/noteman_wcs/`:

```text
noteman_wcs/
  domain.py         # Workspace, Project, Note, Source, Locator, CaptureFragment, Asset
  storage.py        # FileProjectRepository: folders, JSON sidecars, Markdown export
  extraction.py     # extraction methods
  image_capture.py  # image/screenshot capture path
  prompts.py        # prompt template workbench over the local prompt corpus
  desktop_app.py    # Tkinter desktop shell (Ubuntu)
  tools/            # command-line entry points
```

The Windows counterpart lives in `apps/windows-dotnet` (`Noteman.Core` domain/persistence, `Noteman.Desktop` WPF shell) and shares the same storage contract rather than the same code.

## 7. Suggested Data Model

The next version can remain file-based while becoming much more structured.

### Project Metadata

```json
{
  "id": "project-20260628-001",
  "name": "Example Topic",
  "created_at": "2026-06-28T00:00:00",
  "path": "Example Topic",
  "default_note_format": "markdown"
}
```

### Capture Fragment

```json
{
  "id": "fragment-001",
  "note_id": "note-20260628-001",
  "source": {
    "label": "Book or PDF name",
    "type": "book"
  },
  "locator": {
    "kind": "page",
    "value": "12"
  },
  "method": "clipboard_text",
  "text": "Captured text here.",
  "asset_id": null,
  "created_at": "2026-06-28T00:00:00"
}
```

### Markdown Export

```markdown
## Book or PDF name, p. 12

Captured text here.
```

The plain text style already used by the project can be preserved, but metadata should exist either inline or in sidecar JSON.

## 8. Evolution Roadmap

### Phase 1: Stabilize Current Behavior

- Pick `nsu62b.py` as the leading Python branch because it contains the richest feature set.
- Replace global state with a small `AppState` object.
- Replace placeholder-driven validation with explicit state checks.
- Use `pathlib.Path` for cross-platform paths.
- Normalize all captured text to `str`; avoid storing OCR output as `bytes`.

### Phase 2: Introduce Domain Models

- Create models for `Project`, `Note`, `Source`, `Locator`, `Fragment`, and `Asset`.
- Convert `paste`, `imgps`, `clpocr`, and `ocr_read` into fragment producers.
- Convert `export` into a note repository operation.

### Phase 3: Restore Knowledge Management

- Bring forward the Visual Basic version's search, browse, and compare capabilities.
- Make review a first-class mode, not just "open in external editor".
- Add fragment-level search across all notes in a project.

### Phase 4: Improve Source Fidelity

- Store image assets rather than deleting them by default.
- Keep OCR confidence and source filename where available.
- Support page ranges and source types.
- Add deduplication for repeated OCR/paste fragments.

### Phase 5: Modernize Interface

- Keep the fast capture workflow: folder, note, reference, page, paste/OCR, export.
- Add a review panel for existing notes and fragments.
- Show project state clearly: selected project, note, dirty buffer, last export.
- Make OCR configuration part of settings, not a per-session button.

## 9. Design Principle

NoteMan should evolve as a research capture system, not merely a text box with export buttons.

The essential object is not the file. The essential object is the referenced fragment: a piece of knowledge, captured from a source, located within that source, cleaned or extracted by a method, and committed into a note for later retrieval.

## 10. Realization Status (July 2026)

What this ontology proposed and what now exists:

| Concept | Status | Where |
|---|---|---|
| Domain entities (Workspace, Project, Note, Source, Locator, CaptureFragment, Asset) | Realized | `apps/ubuntu-python/src/noteman_wcs/domain.py` |
| Fragment invariant (text or recoverable asset) | Enforced in code | `CaptureFragment.__post_init__` |
| Shared file-based storage (JSON sidecars + Markdown export) | Realized | `apps/ubuntu-python/src/noteman_wcs/storage.py`, `apps/ubuntu-python/docs/WORKSPACE_FORMAT.md` |
| AI draft compartment (`ai_corpus/`, `method: ai_draft`) | Realized | storage contract and repository |
| Prompt template corpus | Realized | `apps/ubuntu-python/src/noteman_wcs/prompts/` |
| Desktop capture shell | Realized on Ubuntu (Tkinter) and scaffolded on Windows (WPF) | `apps/ubuntu-python`, `apps/windows-dotnet` |
| Local screenshot reader (Tesseract + Ollama vision) | In progress — current objective | `apps/ubuntu-python/NEXT_OBJECTIVE.md` |
| Review mode (fragment search, compare, bibliography) | Not yet restored | Phase 3 of the roadmap |
| Asset preservation with OCR confidence | Partially (assets copied, confidence not yet stored) | Phase 4 of the roadmap |

The evolution roadmap in Section 8 remains the reference plan; `DIRECTION_OF_REFINEMENT.md` tracks the live status and ordering of the phases across repositories.
