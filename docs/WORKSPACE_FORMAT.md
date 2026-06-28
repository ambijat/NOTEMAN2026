# NoteMan Workspace Format

Python and C# implementations share this storage contract.

```text
Workspace/
  ProjectName/
    project.json
    assets/
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

## Fragment

A fragment stores:

- `id`
- `text`
- `source`
- `locator`
- `method`
- `asset_id`
- `created_at`

Invariant: a fragment must have text or a recoverable asset reference.

## Markdown

Markdown export is a readable representation, not the only durable store.

```markdown
# Note Title

## Source Label, p. 12

Captured text.
```
