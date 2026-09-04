# NOTEMAN2026

NOTEMAN2026 is the single canonical repository for the NoteMan product family.
The repository name identifies the product; implementation directories identify
the platform.

## Download and explore

- **[Download NoteMan for Windows](https://github.com/ambijat/NOTEMAN2026/releases/download/v0.1.0/Noteman.Desktop.exe)**
- [View the NoteMan product showcase](https://ambijat.github.io/NOTEMAN2026/)
- [Release notes and checksum](https://github.com/ambijat/NOTEMAN2026/releases/tag/v0.1.0)

The Windows download is a self-contained x64 executable. No separate .NET
installation is required.

## Canonical layout

| Path | Role | Status |
| --- | --- | --- |
| `apps/ubuntu-python/` | Ubuntu/Linux Python and Tkinter GUI | Active |
| `apps/windows-dotnet/` | Windows .NET and WPF desktop GUI | Active |

There is one product repository with two active platform implementations.

## Principal implementation by platform

- Ubuntu or Linux work starts in `apps/ubuntu-python/`.
- Windows desktop work starts in `apps/windows-dotnet/`.
- Cross-platform behavior is kept aligned through the parity handoff documents
  shipped with both active implementations.

## Run the Ubuntu/Python application

```bash
cd apps/ubuntu-python
PYTHONPATH=src python3 -m noteman_wcs.desktop_app
```

The Python package requires Python 3.10 or newer.
