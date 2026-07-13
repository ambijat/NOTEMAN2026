# NoteMan WCS Ontology

This ontology contextualizes the old Visual Basic, Python, and C# NoteMan versions as one evolving idea: source-aware research capture.

The system should be designed as a deliberate research loop, not an opaque automation chain. Each stage should inherit durable state from the previous stage, expose its boundary to the user, and stop only at a condition the researcher can verify.

## Historical Lineage

The Visual Basic version established the first complete knowledge-management shape:

- create a text note in a selected folder
- accumulate clipboard text in a working area
- export the working area to a file
- browse folders and files
- open two notes side by side
- search text files

The Python versions narrowed the interface to fast capture:

- select or create a project folder
- create timestamped note files
- paste clipboard text with `Reference{Page}`
- OCR images from a folder
- OCR clipboard images in later variants
- export and reset the working buffer

The C# conversion in `NOTEMAN` proves the same workflow can move beyond Tkinter, but it still carries the one-window, widget-driven structure.

## Durable Entities

The future system should be organized around these concepts:

- `Workspace`: the root location for projects and configuration
- `Project`: a research topic, book, course, chapter, paper, or thesis area
- `Note`: a durable document made from source-aware fragments
- `Source`: the origin of captured material
- `Locator`: page, page range, timestamp, section, URL, or file position
- `CaptureFragment`: the atomic unit of research note-making
- `Asset`: image, screenshot, PDF, audio, or other source material
- `Extraction`: the method that turns source material into text
- `PromptTemplate`: a local editable instruction that turns a captured fragment
  into an AI prompt while preserving source and locator context
- `Export`: the commit of draft material into durable notes
- `Review`: search, compare, read, and reuse captured fragments

## Relationship Spine

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
Export commits CaptureFragment into Note
Review queries Note and CaptureFragment
```

The practical workflow is:

```text
Source -> Locator -> Extraction -> Fragment -> Note -> Review
```

## Invariants

- A fragment must have text or a recoverable asset.
- Every note belongs to one project.
- Every project belongs to one workspace.
- OCR text must preserve its source and locator.
- Reset must not discard unexported work silently.
- Export should prevent accidental duplicate commits.
- Search should work across fragments, not only raw files.
- External editors are optional; the app must remain able to read its own notes.
- Workflow state must be durable enough that a session can be resumed without losing the source trail.
- AI-generated text remains draft material until explicitly reviewed.
- Prompt groups classify prompt tasks for selection; they do not change source,
  draft, note, or export state.
- Export is the convergence point of the workflow, not merely a file write.
- Automated actions must have visible boundaries, recoverable state, and user-controlled final acceptance.
- The system must preserve researcher comprehension rather than replacing it with hidden automation.

## Reinvention

The old apps treated the file as the main object. WCS treats the referenced fragment as the main object.

That shift makes future features natural:

- fragment-level search
- source bibliography
- page-level review
- plagiarism-aware note discipline
- OCR confidence and cleanup state
- export to Markdown, plain text, JSON, or citation-aware formats
- GUI, CLI, and web shells over the same core model
