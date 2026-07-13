# NOTEMAN2026

NOTEMAN2026 is the single canonical repository for the NoteMan product family.
The repository name identifies the product; implementation directories identify
the platform. The former repository names are retained only as Git history and
archive provenance.

## Canonical layout

| Path | Role | Status |
| --- | --- | --- |
| `apps/ubuntu-python/` | Ubuntu/Linux Python and Tkinter implementation, formerly `NOTEMAN8WCS` | Active |
| `apps/windows-dotnet/` | Windows .NET and WPF implementation, formerly `noteman-desktop` | Active |
| `archive/legacy-noteman/` | Original `NOTEMAN` repository and lineage | Frozen reference |
| `archive/workspace-snapshot/` | Historical standalone scripts and binaries from the pre-consolidation workspace | Frozen reference |

There are no longer three competing NoteMan repositories in this workspace.
There is one product repository with two active platform implementations and
an explicitly separated historical archive.

## Principal implementation by platform

- Ubuntu or Linux work starts in `apps/ubuntu-python/`.
- Windows desktop work starts in `apps/windows-dotnet/`.
- Cross-platform behavior is kept aligned through the parity handoff documents
  shipped with both active implementations.
- New product work must not begin in `archive/`.

## Run the Ubuntu/Python application

```bash
cd apps/ubuntu-python
PYTHONPATH=src python3 -m noteman_wcs.desktop_app
```

The Python package requires Python 3.10 or newer.

## Repository history

The consolidation imports the original histories of `ambijat/NOTEMAN`,
`ambijat/NOTEMAN8WCS`, and `ambijat/noteman-desktop`. Their former names may
therefore appear in historical documentation, but the canonical repository and
current product identity are `ambijat/NOTEMAN2026`.
