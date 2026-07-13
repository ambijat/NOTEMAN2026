# Repository Strategy

## Existing Repositories

### `ambijat/NOTEMAN`

Role: legacy archive and lineage record.

Retain:

- `nsu6.py`: first Python capture workflow
- `nsu62.py`: Linux-oriented OCR folder workflow
- `nsu62w.py`: Windows/Tesseract-location workflow
- `nsu63w.py`: Windows clipboard-image OCR workflow
- `nsu62.cs`: C# translation reference

Dump or quarantine:

- `nmtext.py`: unrelated prime-number exercise, not part of NoteMan
- duplicate experimental variants once their unique behavior is documented
- compiled executables if source exists and releases can hold binaries instead

Recommended action:

- keep the repo public as `NOTEMAN Legacy`
- add a lineage README
- avoid adding new feature work there
- use it as the source archive for behavioral archaeology

### `noteman-wcs`

Role: modern successor.

Retain:

- the research-student purpose from the original README
- the C# ambition as a future GUI option

Change:

- make this the ontology, storage, and reference implementation repo
- keep GUI separate from domain logic
- store notes as Markdown plus metadata

## Proposed New Repositories

These should be created when the core is stable enough to split.

### `noteman-desktop`

Windows C# desktop GUI.

Responsibilities:

- capture panel
- review/search panel
- OCR controls
- project browser
- settings

Should depend on WCS core concepts and share the same workspace format as the Python implementation.

## Principal Working Space By Platform

When a platform is chosen, the principal working repository must be chosen with it:

- Ubuntu / Linux work belongs first in `noteman-wcs`.
- Windows desktop work belongs first in `noteman-desktop`.
- Legacy behavior research may consult `ambijat/NOTEMAN`, but new feature work should not begin there.

This keeps implementation work anchored to the platform where it can be built, tested, and understood most directly.

## Cross-Platform Functional Parity

The Python and Windows implementations should be true functional copies of each other, even when their languages, UI toolkits, and operating-system integrations differ.

After a meaningful `noteman-wcs` session completes, the session should leave a platform parity handoff for `noteman-desktop`. The handoff must describe:

- the exact user-visible behavior added or changed
- the domain and storage effects that Windows must reproduce
- the UI states, labels, warnings, and export rules that must match
- the tests or manual checks used to prove the behavior on Ubuntu / Linux
- the Windows-specific substitutions that are allowed without changing the function

The Windows task should begin from that handoff, not from memory or reinterpretation. The aim is not identical source code. The aim is identical research behavior over the same workspace format.

### `NOTEMAN-OCR`

Extractor adapters.

Responsibilities:

- Tesseract integration
- clipboard image support
- folder image OCR
- PDF image extraction
- OCR cleanup and confidence metadata

### `NOTEMAN-RESEARCH-KIT`

Research workflow templates and student-facing guidance.

Responsibilities:

- anti-plagiarism note-taking workflows
- source templates
- thesis/chapter note structures
- examples for books, articles, lectures, and PDFs

## Decision

Do not create many empty repos immediately. Empty repos create administrative weight without architecture. Build `noteman-wcs` until the boundaries become real, then split out desktop, OCR, and research-kit repositories.
