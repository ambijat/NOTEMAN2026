# Project Proposal: Ollama Screenshot Reader for NoteMan WCS

## Working Title

NoteMan Screenshot Capture for Ubuntu

## Objective

Build a Python workflow that reads screenshots or page images on Ubuntu, extracts useful text locally, and stores the result as source-aware NoteMan fragments.

The output must stay compatible with the shared workspace format used by both:

- Python `noteman-wcs`
- Windows C# `noteman-desktop`

## Problem

The original NoteMan scripts helped capture text from clipboard and OCR, but they treated the note file as the main object. The new WCS ontology treats a captured fragment as the main object.

Screenshots need the same discipline:

```text
Source -> Locator -> Extraction -> Fragment -> Note -> Review
```

Without source and locator metadata, screenshot text becomes hard to verify, cite, review, or reuse safely.

## Proposed Solution

Create an Ubuntu-first Python tool that accepts a screenshot/image and creates a NoteMan fragment.

The tool should support two local extraction modes:

- `tesseract`: exact OCR text extraction
- `ollama`: local vision model interpretation, cleanup, and summary

The first version can be command-line only. A GUI can come later.

## Scope

### In Scope

- process one local image at a time
- copy image into the project `assets/` folder
- create or update a project
- create or update a note
- save fragment metadata as JSON
- export readable Markdown
- record extraction method
- preserve source and locator

### Out of Scope for First Version

- live screen capture
- Windows clipboard OCR
- cloud APIs
- batch folder watching
- bibliography export
- PDF page rendering

## Safety and Privacy

The default implementation should be local-first.

Screenshots may contain private data. The tool should not send image content to external services unless the user explicitly chooses that later.

Recommended local stack:

- Tesseract for exact OCR
- Ollama vision model for interpretation and cleanup

## Candidate Command

```bash
python -m noteman_wcs.tools.capture_screenshot \
  --workspace ~/NoteManWorkspace \
  --project "Thesis Notes" \
  --note "Chapter One" \
  --source "Research Book" \
  --locator "12" \
  --image ~/Pictures/page12.png \
  --mode tesseract
```

Possible Ollama mode:

```bash
python -m noteman_wcs.tools.capture_screenshot \
  --workspace ~/NoteManWorkspace \
  --project "Thesis Notes" \
  --note "Chapter One" \
  --source "Research Book" \
  --locator "12" \
  --image ~/Pictures/page12.png \
  --mode ollama \
  --model llava
```

## Output

The tool should produce the shared workspace layout:

```text
Workspace/
  ProjectName/
    project.json
    assets/
      asset-id.png
    notes/
      note-id.md
      note-id.json
```

Markdown example:

```markdown
# Chapter One

## Research Book, p. 12

Extracted screenshot text or summary.
```

## First Implementation Plan

1. Add an asset-copy helper to storage.
2. Add a small CLI entry point under `noteman_wcs.tools`.
3. Implement a stub extractor interface.
4. Implement Tesseract mode first if available.
5. Implement Ollama mode through the local HTTP API.
6. Add tests for workspace output shape.
7. Document Ubuntu setup steps.

## Acceptance Criteria

- runs on Ubuntu with Python
- does not require Windows dependencies
- can create a source-aware fragment from one image
- keeps the original image as an asset
- exports Markdown and JSON
- passes automated tests

## Future Enhancements

- folder batch mode
- clipboard image capture on Linux
- screenshot hotkey workflow
- OCR cleanup prompt templates
- confidence metadata
- image region selection
- Windows C# app integration through the same workspace format
