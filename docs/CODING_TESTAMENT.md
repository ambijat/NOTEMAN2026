# NoteMan Coding Testament

Read this before changing NoteMan Desktop.

NoteMan is not merely a note-taking app. It is a compartmentalized research ethics tool. Its purpose is to help a researcher capture, transform, review, and export material without confusing source text, AI output, and human judgment.

NoteMan should not become an invisible automation pipeline. It may assist extraction, prompting, transformation, and review, but every workflow must preserve state, make transitions visible, and converge on a verifiable research condition: source preserved, locator attached, draft reviewed, and export intentionally accepted.

## Core Principle

Keep each kind of material in its own compartment:

```text
Source text
AI prompt
AI result
Human-reviewed note
Exported note
```

The app should make movement between compartments deliberate and visible.

## Canon

1. Preserve the source trail.
   Every captured fragment should keep its source and locator whenever possible.

2. Keep clipboard interoperability as the primary bridge.
   Copy-paste is simple, inspectable, model-independent, and safer than hidden automation.

3. Do not auto-send source material to AI.
   The user must choose when text leaves NoteMan through the clipboard.

4. Do not auto-save AI output as final research material.
   AI output belongs in `Typed / AI Draft` until the user reviews it.

5. Keep original capture separate from AI transformation.
   A source fragment and an AI-generated paraphrase are different research objects.

6. Make export intentional.
   Exporting should mean the user has accepted the note state as durable.

7. Prefer transparent workflows over clever automation.
   If a feature hides where text came from or where it went, redesign it.

8. Keep prompts local and editable.
   Prompt templates should remain plain text files where possible.

9. Avoid vendor lock-in.
   The app should work with ChatGPT, Claude, Ollama, or any other tool that accepts clipboard text.

10. Support ethical research habits.
    The app should help avoid plagiarism, accidental unattributed copying, and unreviewed AI prose.

11. Build brakes before automation.
    Automated actions must have visible boundaries, recoverable state, and user-controlled final acceptance.

12. Keep the researcher as checker.
    The app may help produce drafts, but it must not grade its own output as final research material.

## Design Shape

The intended workflow is:

```text
Capture source fragment
-> Build prompt from fragment
-> Copy prompt to AI
-> Paste AI result into Typed / AI Draft
-> Human reviews and edits
-> Save Draft as Fragment only when accepted
-> Export Note intentionally
```

## Practical Rule For Future Coding

Before adding a feature, ask:

```text
Does this preserve compartments, source trail, and deliberate user action?
```

If the answer is no, the feature should be simplified or redesigned.
