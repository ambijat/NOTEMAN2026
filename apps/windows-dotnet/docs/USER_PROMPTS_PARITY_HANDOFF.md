# User Prompts Platform Parity Handoff

## Source Session

- Source repo: `apps/windows-dotnet`
- Target repo: `apps/ubuntu-python`
- Platform implemented first: Windows
- Date: 2026-07-12
- Feature or workflow name: Temporary and persistent user-defined prompts

## User-Visible Behavior To Copy

- Entry point: prompt controls below the prompt dropdown
- Buttons: `Add Prompt` and `Remove User Prompt`
- Adding asks for a name, prompt text, and whether to keep it after closing.
- Added prompts appear immediately in the `User` group and are selected.
- A session-only prompt disappears when the app closes.
- Removing a user prompt asks for confirmation; bundled prompts cannot be removed.

## Domain And Storage Behavior

- Prompt templates remain separate from notes and captured fragments.
- Persistent prompts use the existing text format: title, `Group: User`, then body.
- Persistent prompts belong in the platform user configuration directory, outside
  both the installed prompt corpus and research workspace.
- Duplicate prompt titles are rejected case-insensitively.

## Verification Checklist

- [ ] Add and immediately select a session-only prompt.
- [ ] Confirm a session-only prompt is absent after restart.
- [ ] Add a persistent prompt and confirm it reloads after restart.
- [ ] Remove both temporary and persistent user prompts.
- [ ] Confirm built-in prompts cannot be removed through this control.
- [ ] Run the Python test suite and compilation checks.
