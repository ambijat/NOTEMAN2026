# Platform Parity Handoff Template

Use this at the end of a meaningful `apps/ubuntu-python` session when the same behavior should later be implemented in `apps/windows-dotnet`.

The handoff exists so the Windows version can emulate the Python version's function without guessing. Different OS APIs, language constructs, and UI controls are acceptable only when the research behavior remains the same.

## Source Session

- Source repo: `apps/ubuntu-python`
- Target repo: `apps/windows-dotnet`
- Platform implemented first: Ubuntu / Linux
- Date:
- Commit:
- Feature or workflow name:

## User-Visible Behavior

Describe the exact behavior the Windows app must copy.

- Entry point:
- Buttons, menus, or commands:
- Required labels and status messages:
- Error or warning messages:
- Completion condition:

## Domain Behavior

Describe the behavior independent of GUI toolkit or language.

- Entities affected:
- State transitions:
- Validation rules:
- Invariants preserved:
- Actions that must remain user-controlled:

## Storage And Workspace Effects

Describe every file or metadata effect that must match.

- Workspace files read:
- Workspace files written:
- JSON / Markdown fields added or changed:
- Compatibility expectations:
- Migration or fallback behavior:

## Platform-Specific Substitutions

List allowed differences that do not change the function.

- Ubuntu / Python implementation detail:
- Windows / C# equivalent:
- Acceptable UI difference:
- Unacceptable behavior change:

## Verification Used In `apps/ubuntu-python`

- Automated checks:
- Manual checks:
- Sample workspace or fixture:
- Known limitation:

## Windows Implementation Checklist

- [ ] Same workflow is reachable in `apps/windows-dotnet`.
- [ ] Same source, locator, draft, review, and export compartments are preserved.
- [ ] Same workspace format is read and written.
- [ ] Same warnings and final acceptance rules exist.
- [ ] Windows-specific code changes do not create a different research workflow.
- [ ] Windows verification results are recorded before the task is closed.

## Notes For The Windows Session

Add direct implementation guidance for the next `apps/windows-dotnet` task.
