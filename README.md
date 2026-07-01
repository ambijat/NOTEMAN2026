# noteman-desktop

Windows desktop implementation of NoteMan as a source-aware Workspace Capture System.

This repository is the C# companion to `noteman-wcs`. The two implementations may have different interfaces, but they should read and write the same workspace format.

When Windows work follows a completed Python session, begin from a platform parity handoff. The goal is not identical source code; it is identical research behavior across OS and language boundaries.

Before changing the app, read `docs/CODING_TESTAMENT.md`. It records the research-ethics and compartmentalization principles that guide the design.

## Goal

NoteMan Desktop should make fast research capture natural on Windows:

- create or open a workspace
- create projects and notes
- capture clipboard text with source and locator
- preserve fragments as structured JSON
- export notes as Markdown
- prepare clean extension points for clipboard OCR and image OCR

The core object is a captured fragment:

```text
Source -> Locator -> Extraction -> Fragment -> Note -> Review
```

## Projects

- `src/Noteman.Core`: domain model and file-based persistence
- `src/Noteman.Desktop`: WPF desktop shell
- `docs/PLATFORM_PARITY_HANDOFF_TEMPLATE.md`: Windows parity checklist for mirroring completed `noteman-wcs` behavior

## Build

Install the .NET SDK on Windows, then run:

```powershell
dotnet build src\Noteman.Desktop\Noteman.Desktop.csproj
dotnet run --project src\Noteman.Desktop\Noteman.Desktop.csproj
```

The current machine used to scaffold this repo did not have `dotnet` installed, so the initial project was created by hand and not compiled locally.
