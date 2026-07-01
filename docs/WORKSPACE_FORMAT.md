# NoteMan Workspace Format

Python and C# implementations share this storage contract.

```text
Workspace/
  ProjectName/
    project.json
    assets/
    ai_corpus/
      note-id-fragment-id.md
      note-id-fragment-id.json
    notes/
      note-id.md
      note-id.json
```

## Project

`project.json` stores:

- `id`
- `name`
- `created_at`

## Note

Each note has a Markdown export and a JSON sidecar.

The JSON sidecar stores:

- `id`
- `title`
- `created_at`
- `fragments`

## Assets

`assets/` stores recoverable source inputs such as screenshots, scanned pages,
PDF pages, clipboard images, and other files that produced fragments.

Text-only capture and AI draft workflows may leave this folder empty.

## AI Corpus

`ai_corpus/` stores reviewed AI draft outputs created by `Save (AI) Draft`.
These files are AI-derived research material, not original source assets and not
the final exported note.

Each AI corpus entry has a Markdown file and JSON sidecar.

The JSON sidecar stores:

- `id`
- `note_id`
- `note_title`
- `fragment`
- `created_at`

## Fragment

A fragment stores:

- `id`
- `text`
- `source`
- `locator`
- `method`
- `asset_id`
- `created_at`

AI draft fragments use `method: "ai_draft"`.

Invariant: a fragment must have text or a recoverable asset reference.

## Markdown

Markdown export is a readable representation, not the only durable store.

```markdown
# Note Title

## Source Label, p. 12

Captured text.
```
