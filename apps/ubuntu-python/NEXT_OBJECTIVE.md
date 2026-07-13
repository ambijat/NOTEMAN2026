# Next Objective: Ubuntu Ollama Screenshot Reader

Build the Python-side screenshot reader for NoteMan WCS.

## Goal

Create a local Ubuntu workflow that turns screenshots into source-aware NoteMan fragments:

```text
screenshot/image -> local OCR or Ollama vision -> cleaned text/summary -> CaptureFragment -> Markdown + JSON note
```

## Why This Comes First

- Ubuntu is already the comfortable Python/OCR environment.
- Ollama can stay local, so screenshots do not need a cloud service.
- The output can use the same workspace format that the Windows C# app reads and writes.
- This keeps Python and C# parallel without making either one depend on the other.

## First Milestone

Create a command-line script that accepts:

```bash
python -m noteman_wcs.tools.capture_screenshot \
  --workspace ~/NoteManWorkspace \
  --project "Thesis Notes" \
  --note "Chapter One" \
  --source "Book Screenshot" \
  --locator "p. 12" \
  --image ~/Pictures/page12.png
```

Expected result:

- image copied into the project `assets/` folder
- extracted or summarized text saved as a `CaptureFragment`
- note exported as Markdown
- note sidecar saved as JSON

## Safety Rule

Treat screenshots as sensitive. Do not send them to cloud APIs by default. The first implementation should target local tools only:

- Tesseract for exact OCR
- Ollama vision model for cleanup, structure, and summary

## Proposed Files

- `src/noteman_wcs/tools/capture_screenshot.py`
- `src/noteman_wcs/ollama_client.py`
- `tests/test_screenshot_capture.py`
- `docs/OLLAMA_SCREENSHOT_PROPOSAL.md`

## Done When

- the script can process one local image
- output follows `docs/WORKSPACE_FORMAT.md`
- Python tests pass on Ubuntu
- no Windows-only dependency is introduced
