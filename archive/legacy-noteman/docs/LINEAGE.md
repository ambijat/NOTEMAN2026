# NoteMan Lineage

The code in this repository records the evolution from a direct GUI utility into a research capture system.

## Conceptual Milestones

### Capture

The earliest Python versions established the central habit:

```text
choose folder -> create note -> paste text -> attach reference/page -> export
```

### OCR

Later versions added Tesseract-backed image extraction. This moved the tool from simple clipboard capture into source conversion.

### Clipboard Image OCR

The Windows and later Linux-oriented variants introduced clipboard-image OCR. This is the ancestor of the modern WCS idea of an `Extraction` producing a `CaptureFragment`.

### C# Translation

`nsu62.cs` demonstrates that the workflow is not bound to Python/Tkinter. It should be treated as a UI/platform reference, not the final architecture.

## Lessons To Carry Forward

- Fast capture matters.
- Page/source markers are the soul of the tool.
- OCR must preserve source context.
- Export/reset is really a state machine.
- Browse/search/review should return, because the Visual Basic version already understood that note-making continues after capture.

## Successor Ontology

The successor project should model:

- Workspace
- Project
- Note
- Source
- Locator
- CaptureFragment
- Asset
- Extraction
- Export
- Review

The main object should be the referenced fragment, not the GUI text box and not even the text file.
