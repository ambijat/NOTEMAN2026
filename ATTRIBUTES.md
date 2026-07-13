# NoteMan Attribute Catalog

Companion to `ONTOLOGICAL_BASIS.md`. That document defines the entities; this one enumerates their attributes as they actually exist in code and storage, together with the legacy widget or variable each attribute descends from.

Sources of truth, in order of authority:

1. `apps/ubuntu-python/src/noteman_wcs/domain.py` — the dataclasses
2. `apps/ubuntu-python/docs/WORKSPACE_FORMAT.md` — the storage contract shared with C#
3. `apps/ubuntu-python/src/noteman_wcs/storage.py` — the serialization behavior

If a future change makes these disagree, fix the code or the contract, then this catalog.

## Conventions

- **Identity**: entity ids are generated as `<prefix>-<12 hex chars>` by `new_id()` (e.g. `fragment-3fa8c2d19b04`).
- **Time**: all timestamps are UTC ISO-8601 with seconds precision, produced by `utc_now()` (e.g. `2026-07-05T10:15:30+00:00`). The legacy scripts encoded time in filenames (`noteYYMMDDHHMM.txt`); the modern model stores it as data.
- **Mutability**: `Workspace`, `Source`, `Locator`, and `Asset` are frozen dataclasses (values); `Project`, `Note`, and `CaptureFragment` are mutable records.

## Workspace

The root location owning projects and configuration.

| Attribute | Type | Notes |
|---|---|---|
| `path` | `Path` | The only attribute today. Frozen value object. |

Legacy ancestry: VB destination folder (`fp`, `kp`, `u`); Python `filedialog.askdirectory` result.

Planned growth (per the ontology's future role): search indexes, configuration, export policies.

## Project

A named research topic; one folder per project inside the workspace.

| Attribute | Type | Default | Notes |
|---|---|---|---|
| `name` | `str` | required | Also the folder name on disk |
| `id` | `str` | `new_id("project")` | |
| `created_at` | `str` | `utc_now()` | |

Storage: `Workspace/<name>/project.json` with keys `id`, `name`, `created_at`. The repository also guarantees the subfolders `assets/`, `ai_corpus/`, and `notes/` exist.

Legacy ancestry: `tb6` ("Folder Name Here") and `foloc` (actual path).

## Note

A durable document composed of fragments.

| Attribute | Type | Default | Notes |
|---|---|---|---|
| `title` | `str` | required | |
| `id` | `str` | `new_id("note")` | |
| `created_at` | `str` | `utc_now()` | |
| `fragments` | `list[CaptureFragment]` | `[]` | Ordered; appended via `add_fragment()` |

Storage: dual representation in `notes/` — `note-id.md` (readable Markdown rendering) and `note-id.json` (durable sidecar with `id`, `title`, `created_at`, `fragments`). The Markdown is a rendering, not the only durable store.

Legacy ancestry: `tb3` filename box; timestamped files `noteYYMMDDHHMM.txt` (Python) and `newDDMMMyyyyhhmm.txt` (VB).

## Source

The origin of captured material. Frozen value object.

| Attribute | Type | Default | Notes |
|---|---|---|---|
| `label` | `str` | required | Human-readable name of the book, article, page, etc. |
| `type` | `SourceType` | `UNKNOWN` | |

`SourceType` values: `unknown`, `book`, `article`, `pdf`, `webpage`, `lecture`, `image`, `clipboard`.

Legacy ancestry: `tb4` ("Reference...") — provenance was once a free-text prefix inside the note body; it is now typed data.

## Locator

A position inside a source. Frozen value object.

| Attribute | Type | Default | Notes |
|---|---|---|---|
| `value` | `str` | `""` | |
| `kind` | `LocatorKind` | `NONE` | |

`LocatorKind` values: `none`, `page`, `page_range`, `timestamp`, `section`, `url`, `file`.

Display rule (`Locator.display()`): empty when kind is `none` or value is blank; `p. <value>` for pages; `<kind>: <value>` otherwise. This drives the citation heading in Markdown exports.

The locator model refines the earlier page-number and inline `Reference{Pgs.}` convention.

## CaptureFragment

The atomic unit of research note-making and the central object of the system.

| Attribute | Type | Default | Notes |
|---|---|---|---|
| `text` | `str` | required | Cleaned captured text |
| `source` | `Source` | required | |
| `locator` | `Locator` | `Locator()` (none) | |
| `method` | `ExtractionMethod` | `MANUAL` | How the text was produced |
| `asset_id` | `str \| None` | `None` | Link to a recoverable Asset |
| `id` | `str` | `new_id("fragment")` | |
| `created_at` | `str` | `utc_now()` | |

`ExtractionMethod` values: `manual`, `ai_draft`, `clipboard_text`, `clipboard_ocr`, `image_ocr`, `pdf_text`.

Enforced invariant (`__post_init__`): a fragment must have non-blank text **or** a recoverable asset reference — otherwise construction raises `ValueError`. This is the modern form of the rule that captured material is never silently lost.

Citation rule (`citation_heading()`): `"<source label>, <locator display>"`, or just the label when there is no locator. Rendered as the `##` heading of each fragment in Markdown export.

Fragments with `method: ai_draft` are AI-derived draft material and belong in the `ai_corpus/` compartment until reviewed; they are not original source captures.

Legacy ancestry: the inline block

```text
Reference{Page}
captured text
```

produced by `paste()`, `imgps()`, `clpocr()`, and `ocr_read()` in the Tkinter scripts.

## Asset

A recoverable non-text input. Frozen value object.

| Attribute | Type | Default | Notes |
|---|---|---|---|
| `path` | `Path` | required | Project-relative, e.g. `assets/asset-<hex>.png` |
| `media_type` | `str` | required | MIME type guessed on import; `application/octet-stream` fallback |
| `id` | `str` | `new_id("asset")` | |

Import behavior (`FileProjectRepository.copy_asset`): the file is copied into the project's `assets/` folder under its asset id with a lower-cased original extension. The original is never moved or deleted — a deliberate reversal of the legacy scripts, which deleted images after OCR.

Legacy ancestry: `.png`/`.jpg` files in `foloc` and `foloc/imgdata`.

## AI Corpus Entry

A reviewed AI draft, stored in its own compartment. Not a domain dataclass yet — it exists as a storage record written by `save_ai_corpus_entry`.

| Attribute | Type | Notes |
|---|---|---|
| `id` | `str` | `<note_id>-<fragment_id>` |
| `note_id` | `str` | Owning note |
| `note_title` | `str` | Denormalized for readability |
| `fragment` | object | Full serialized CaptureFragment (with `method: ai_draft`) |
| `created_at` | `str` | Copied from the fragment |

Storage: `ai_corpus/<note-id>-<fragment-id>.md` plus JSON sidecar of the same name.

## PromptTemplate

A local, editable instruction that renders a fragment into an AI prompt. Lives as plain text files in `apps/ubuntu-python/src/noteman_wcs/prompts/`, managed by `prompts.py`.

Governing rules (from the Coding Testament): the prompt body must remain visible, inspectable text; prompt groups may classify tasks for selection but must not become hidden instructions or change source, draft, note, or export state.

## Attribute-Level Invariants

Rules that constrain attributes across entities:

1. `CaptureFragment.text` may be blank only when `asset_id` points to a recoverable asset (enforced).
2. `Locator` and `Source` accompany OCR output — extraction must never strip provenance.
3. `method` must honestly record how text was produced; AI-derived text is always `ai_draft`, never disguised as capture.
4. Ids and `created_at` are assigned once at creation and never rewritten by storage round-trips (`storage._fragment_from_dict` preserves them).
5. Serialization is tolerant on read (missing `locator`/`method` fall back to defaults) but complete on write (full `asdict` output).

## Gaps Between Ontology and Attributes (candidates for refinement)

Attributes the ontology promises that no entity carries yet:

- OCR **confidence** and cleanup status on fragments (Phase 4 of the roadmap)
- **Export history / audit trail** on notes (export is currently a plain write)
- **Page-range and source-type aware** deduplication keys
- Workspace-level **configuration and search index** attributes
- Note ordering/dirty state — the desktop shells track buffer state in the UI layer; the domain has no `BufferDirty` representation

These gaps are tracked with direction and priority in `DIRECTION_OF_REFINEMENT.md`.
