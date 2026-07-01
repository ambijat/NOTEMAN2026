# NoteMan Workflow Protocol

This protocol is the shared behavior contract for `noteman-wcs` and
`noteman-desktop`. Platform controls may differ, but the research workflow,
visible vocabulary, storage folders, and fragment meanings must stay aligned.

## Hierarchy

```text
Workspace -> Project -> Note -> Fragment
```

- A workspace is the root folder selected by the user.
- A project is a child folder with `project.json`, `notes/`, `assets/`, and
  `ai_corpus/`.
- A note is stored in `notes/<note-id>.json` and `notes/<note-id>.md`.
- A fragment is one captured or drafted unit inside the note JSON.

## Workspace Selection

- `Choose Workspace` should open at the last remembered workspace when one is
  available.
- After a workspace is selected, the app refreshes the project list from
  existing project folders.
- The last workspace preference is convenience state only; it must not change
  the workspace storage contract.

## Project Selection

- The `Project` control is editable.
- Existing projects are listed from folders that contain `project.json`.
- Selecting an existing project loads its note list.
- Typing a new project name creates or uses that project when the workflow saves
  data.

## Note Selection

- The `Note` control is editable.
- Existing notes are listed from `notes/*.json` for the selected project.
- Selecting an existing note loads its saved fragments.
- `New Note` always creates a fresh note from the typed note name and reports:
  `New note with <name> created.`

## Source Capture

- `Paste Clipboard Text` creates a source fragment with the current source and
  locator metadata.
- Source and non-AI fragments are displayed in `Caputre Text Preview`.
- Source capture text remains visible after export because it is the source
  review area, not a one-time export buffer.

## AI Drafts

- `Paste AI Result` fills `Typed / AI Draft`.
- `Save (AI) Draft` stores the draft as an `ai_draft` fragment and writes a
  corresponding AI corpus entry under `ai_corpus/`.
- Loading an existing note renders saved `ai_draft` fragments in
  `Typed / AI Draft`.
- Loaded AI retrieval text must not be saved or exported again unchanged.
- Pasting a fresh AI result resets the draft state so it can be saved normally.

## Export

- `Export Text/AI Note` writes the whole current note to `notes/<note-id>.md`
  and `notes/<note-id>.json`.
- If `Typed / AI Draft` contains fresh unsaved text, export adds it as a manual
  fragment before writing the note.
- If `Typed / AI Draft` contains unchanged loaded AI retrieval text, export must
  not duplicate it.
- Export does not clear `Caputre Text Preview`; the preview remains available
  for checking and further work.

## Assets And AI Corpus

- `assets/` is for recoverable source files such as screenshots, scanned pages,
  PDF pages, clipboard images, and similar inputs.
- Text-only capture workflows may leave `assets/` empty.
- `ai_corpus/` is the dedicated corpus for reviewed AI-generated drafts created
  by `Save (AI) Draft`.

## Parity Rule

Both app variants must preserve these exact visible labels unless the user
changes the canon:

- `Typed / AI Draft`
- `Caputre Text Preview`
- `Save (AI) Draft`
- `Export Text/AI Note`

Any future feature that changes workspace hierarchy, project selection, note
selection, source capture, AI draft handling, export, `assets/`, or `ai_corpus/`
must be implemented in both repositories or recorded with a parity handoff.

## Validation Before Commit

For `noteman-wcs`:

```bash
python3 -m py_compile src/noteman_wcs/desktop_app.py src/noteman_wcs/storage.py
PYTHONPATH=src python3 -m unittest discover -s tests
```

For `noteman-desktop` on a Windows-capable .NET SDK:

```bash
dotnet build src/Noteman.Core/Noteman.Core.csproj
dotnet build src/Noteman.Desktop/Noteman.Desktop.csproj
```

On Linux hosts without Windows Desktop SDK targets, validate
`src/Noteman.Core/Noteman.Core.csproj` and record the WPF build limitation.
