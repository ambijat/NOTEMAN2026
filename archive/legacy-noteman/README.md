# NOTEMAN Legacy

This repository is the historical archive for the NoteMan/NoteMaker experiments.

The active successor is `noteman-wcs`, which rethinks NoteMan as a Workspace Capture System built around source-aware research fragments.

## What This Repo Contains

- `nsu6.py`: early Python/Tkinter note capture workflow
- `nsu62.py`: Linux-oriented OCR folder workflow
- `nsu62w.py`: Windows-oriented variant with Tesseract path selection
- `nsu63w.py`: Windows clipboard-image OCR variant
- `nsu62.cs`: C# translation/reference implementation

## What Was Removed

- `nmtext.py` was an unrelated prime-number exercise and not part of NoteMan.

## Preservation Policy

This repository should preserve historical behavior, not receive major new features.

Retain files that show a distinct capability:

- folder/project selection
- timestamped note creation
- reference and page marking
- clipboard capture
- image OCR
- clipboard-image OCR
- export/reset workflow
- cross-platform experiments

Move future design and Python/shared-format work to `noteman-wcs`. Move Windows C# desktop work to `noteman-desktop`.
