# NOTEMAN2026 — Ubuntu/Python

This directory is the Ubuntu/Python implementation of NOTEMAN2026 as a
Workspace Capture System.

It is the **Python/Linux reference implementation and shared behavioral
contract**. The authoritative Windows product lives in `apps/windows-dotnet`.

The old NoteMan programs proved the core workflow: select a folder, create a note, paste or OCR material, attach a reference/page marker, export it, and later review it. This repository rethinks that idea as a research-note architecture built around referenced fragments rather than plain text files alone.

Before changing this repository, read `docs/CODING_TESTAMENT.md`. It records the research-ethics and compartmentalization principles that guide the project.

## Core Idea

The central object is a captured fragment:

```text
Source -> Locator -> Extraction -> Fragment -> Note -> Review
```

A fragment is a piece of captured knowledge with:

- source label, such as a book, article, PDF, lecture, webpage, or screenshot
- locator, such as page number, range, timestamp, section, or image filename
- extraction method, such as manual entry, clipboard text, Tesseract OCR, or future PDF/audio import
- cleaned text
- optional asset reference

## Repository Role

This repo should become the modern successor to the one-file GUI experiments.

Current Ubuntu objective: build the local Ollama/Tesseract screenshot reader described in `NEXT_OBJECTIVE.md`.

It contains:

- `src/noteman_wcs/domain.py`: domain model for workspaces, projects, notes, sources, locators, fragments, and assets
- `src/noteman_wcs/storage.py`: file-based repository for projects, metadata, and markdown export
- `src/noteman_wcs/desktop_app.py`: Ubuntu/Python Tkinter desktop shell matching the Windows workflow
- `src/noteman_wcs/prompts/`: local editable prompt corpus used by the clipboard prompt workbench
- `NEXT_OBJECTIVE.md`: immediate Ubuntu/Python development target
- `docs/CODING_TESTAMENT.md`: read-before-coding design canon
- `docs/ONTOLOGY.md`: contextual ontology derived from the old Visual Basic, Python, and C# versions
- `docs/OLLAMA_SCREENSHOT_PROPOSAL.md`: project proposal for local screenshot reading
- `docs/WORKSPACE_FORMAT.md`: shared storage contract for Python and C# implementations
- `docs/REPOSITORY_STRATEGY.md`: what to do with old and future repositories
- `docs/PLATFORM_PARITY_HANDOFF_TEMPLATE.md`: session handoff template for keeping Python and Windows behavior aligned

## Repository Ecosystem

The proposed future ecosystem is:

- `NOTEMAN`: legacy archive and historical reference
- `apps/ubuntu-python`: core ontology, storage, and reference implementation
- `apps/windows-dotnet`: Windows C# GUI shell over the WCS core
- `NOTEMAN-OCR`: future OCR/extraction adapters
- `NOTEMAN-RESEARCH-KIT`: future templates, workflows, and documentation for research students

The first three repositories exist today. OCR and research-kit repositories
should be created only when enough implementation exists to justify them.

## Development

This project currently has no external runtime dependencies.

```powershell
python -m compileall src
python -m unittest discover -s tests
```

Run the Python desktop shell:

```bash
PYTHONPATH=src python -m noteman_wcs.desktop_app
```

## Philosophy

NoteMan should help research students avoid plagiarism by making source-aware note-taking easier than careless copying. Its job is not just to store text. Its job is to preserve the path from source to thought.
