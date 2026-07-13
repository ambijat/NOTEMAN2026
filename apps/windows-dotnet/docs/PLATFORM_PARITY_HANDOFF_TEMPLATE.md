# Platform Parity Handoff Template

Use this when implementing Windows behavior that follows a completed `apps/ubuntu-python` session.

The handoff exists so the Windows version can emulate the Python version's function without guessing. Different OS APIs, language constructs, and UI controls are acceptable only when the research behavior remains the same.

## Source Session

- Source repo: `apps/ubuntu-python`
- Target repo: `apps/windows-dotnet`
- Platform implemented first: Ubuntu / Linux
- Date:
- Source commit:
- Feature or workflow name:

## User-Visible Behavior To Copy

- Entry point:
- Buttons, menus, or commands:
- Required labels and status messages:
- Error or warning messages:
- Completion condition:

## Domain Behavior To Copy

- Entities affected:
- State transitions:
- Validation rules:
- Invariants preserved:
- Actions that must remain user-controlled:

## Storage And Workspace Effects To Copy

- Workspace files read:
- Workspace files written:
- JSON / Markdown fields added or changed:
- Compatibility expectations:
- Migration or fallback behavior:

## Windows Substitutions

- Python / Ubuntu implementation detail:
- C# / Windows equivalent:
- Acceptable UI difference:
- Unacceptable behavior change:

## Verification Checklist

- [ ] Same workflow is reachable in `apps/windows-dotnet`.
- [ ] Same source, locator, draft, review, and export compartments are preserved.
- [ ] Same workspace format is read and written.
- [ ] Same warnings and final acceptance rules exist.
- [ ] Windows-specific code changes do not create a different research workflow.
- [ ] Windows build or manual verification result is recorded.

## Implementation Notes

Record how the Windows implementation mirrors the completed Python behavior.
