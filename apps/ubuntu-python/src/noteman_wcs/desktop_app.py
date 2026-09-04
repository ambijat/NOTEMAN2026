"""Tkinter desktop shell for NoteMan WCS.

This is the Ubuntu/Python counterpart to the Windows C# app. It keeps the same
compartments: captured source fragments, prompt workbench, AI draft, accepted
fragments, and intentional export.
"""

from __future__ import annotations

import json
import os
import sys
import tkinter as tk
from pathlib import Path
from tkinter import filedialog, messagebox, ttk

if __package__ in {None, ""}:
    sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
    __package__ = "noteman_wcs"

from .domain import CaptureFragment, ExtractionMethod, Locator, LocatorKind, Note, Project, Source
from .prompts import PromptTemplate, load_prompt_templates, render_prompt
from .storage import FileProjectRepository, render_fragment, render_note_markdown

CONFIG_DIR_ENV = "XDG_CONFIG_HOME"
CONFIG_FILE_NAME = "desktop_app.json"


def desktop_config_path() -> Path:
    config_root = Path(os.environ.get(CONFIG_DIR_ENV, Path.home() / ".config"))
    return config_root / "noteman-wcs" / CONFIG_FILE_NAME


def user_prompt_directory() -> Path:
    return desktop_config_path().parent / "prompts"


def load_last_workspace(config_path: Path | None = None) -> Path | None:
    path = config_path or desktop_config_path()
    try:
        value = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError):
        return None
    last_workspace = value.get("last_workspace") if isinstance(value, dict) else None
    if not isinstance(last_workspace, str) or not last_workspace.strip():
        return None
    return Path(last_workspace).expanduser()


def save_last_workspace(workspace_path: Path, config_path: Path | None = None) -> None:
    path = config_path or desktop_config_path()
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(
        json.dumps({"last_workspace": str(workspace_path.expanduser().resolve())}, indent=2),
        encoding="utf-8",
    )


def workspace_dialog_initial_dir(
    current_workspace: Path | None,
    last_workspace: Path | None,
    fallback: Path | None = None,
) -> Path:
    if current_workspace is not None:
        current = current_workspace.expanduser()
        if current.is_dir():
            return current

    if last_workspace is not None:
        last = last_workspace.expanduser()
        if last.is_dir():
            return last
        if last.parent.is_dir():
            return last.parent

    fallback_dir = fallback or Path.home()
    if fallback_dir.is_dir():
        return fallback_dir
    return Path.home()


class NoteManDesktopApp(tk.Tk):
    def __init__(self) -> None:
        super().__init__()
        self.title("NoteMan Desktop")
        self.geometry("980x640")
        self.minsize(760, 500)

        self.workspace_path: Path | None = None
        self.last_workspace_path: Path | None = load_last_workspace()
        self.current_project: Project | None = None
        self.current_note: Note | None = None
        self.note_choice_index: dict[str, str] = {}
        self.draft_loaded_from_note = False
        self.prompts = load_prompt_templates()
        self.prompts.extend(load_prompt_templates(user_prompt_directory()))

        self._build_ui()
        self._set_status("Select a workspace to begin.")

    def _build_ui(self) -> None:
        self.columnconfigure(0, weight=0)
        self.columnconfigure(1, weight=9)
        self.columnconfigure(2, weight=9)
        self.rowconfigure(0, weight=1)

        left = ttk.Frame(self, padding=(16, 8))
        left.grid(row=0, column=0, sticky="nsew")
        left.columnconfigure(0, minsize=210)

        ttk.Label(left, text="Workspace", font=("", 10, "bold")).grid(sticky="w", pady=(0, 6))
        ttk.Button(left, text="Choose Workspace", command=self.choose_workspace).grid(sticky="ew", pady=(0, 8))
        self.workspace_label = ttk.Label(left, text="", wraplength=230)
        self.workspace_label.grid(sticky="ew", pady=(0, 16))

        ttk.Label(left, text="Project", font=("", 10, "bold")).grid(sticky="w", pady=(0, 6))
        self.project_var = tk.StringVar(value="Thesis Notes")
        self.project_choice = ttk.Combobox(left, textvariable=self.project_var, values=[], state="normal")
        self.project_choice.grid(sticky="ew", pady=(0, 8))
        self.project_choice.bind("<<ComboboxSelected>>", self._refresh_note_choices)
        self.project_choice.bind("<FocusOut>", self._refresh_note_choices)
        self.project_choice.bind("<Return>", self._refresh_note_choices)

        ttk.Label(left, text="Note", font=("", 10, "bold")).grid(sticky="w", pady=(0, 6))
        self.note_var = tk.StringVar(value="Chapter One")
        self.note_choice = ttk.Combobox(left, textvariable=self.note_var, values=[], state="normal")
        self.note_choice.grid(sticky="ew", pady=(0, 8))
        self.note_choice.bind("<<ComboboxSelected>>", self._load_selected_note)
        ttk.Button(left, text="New Note", command=self.new_note).grid(sticky="ew", pady=(0, 8))
        ttk.Button(left, text="Retrieve Note", command=self.retrieve_note).grid(sticky="ew", pady=(0, 16))

        ttk.Label(left, text="Source", font=("", 10, "bold")).grid(sticky="w", pady=(0, 6))
        self.source_var = tk.StringVar(value="Reference...")
        ttk.Entry(left, textvariable=self.source_var).grid(sticky="ew", pady=(0, 8))

        ttk.Label(left, text="Page / Locator", font=("", 10, "bold")).grid(sticky="w", pady=(0, 6))
        page_frame = ttk.Frame(left)
        page_frame.grid(sticky="ew", pady=(0, 16))
        page_frame.columnconfigure(1, weight=1)
        ttk.Button(page_frame, text="-", width=3, command=lambda: self.change_page(-1)).grid(row=0, column=0)
        self.locator_var = tk.StringVar(value="1")
        ttk.Entry(page_frame, textvariable=self.locator_var, justify="center").grid(row=0, column=1, sticky="ew")
        ttk.Button(page_frame, text="+", width=3, command=lambda: self.change_page(1)).grid(row=0, column=2)

        ttk.Button(left, text="Paste Clipboard Text", command=self.paste_clipboard_text).grid(sticky="ew", pady=(0, 8))
        ttk.Button(left, text="Clipboard OCR (soon)", command=self.clipboard_ocr).grid(sticky="ew", pady=(0, 8))
        ttk.Button(left, text="Undo Last Capture", command=self.undo_last_capture).grid(sticky="ew", pady=(0, 8))
        ttk.Button(left, text="Export Text/AI Note", command=self.export_note).grid(sticky="ew", pady=(0, 8))
        ttk.Button(left, text="Clear Typed Draft", command=self.clear_typed_draft).grid(sticky="ew", pady=(0, 14))
        ttk.Separator(left).grid(sticky="ew", pady=(0, 10))
        ttk.Label(left, text="Prompt Library", font=("", 10, "bold")).grid(sticky="w", pady=(0, 6))
        ttk.Button(left, text="Add Prompt", command=self.add_prompt).grid(sticky="ew", pady=(0, 6))
        ttk.Button(left, text="Remove User Prompt", command=self.remove_prompt).grid(sticky="ew")

        middle = ttk.Frame(self, padding=(0, 10, 10, 10))
        middle.grid(row=0, column=1, sticky="nsew")
        middle.rowconfigure(1, weight=1)
        middle.columnconfigure(0, weight=1)
        ttk.Label(middle, text="Typed / AI Draft", font=("", 10, "bold")).grid(sticky="w", pady=(0, 6))
        draft_frame, self.draft = self._scrolled_text(middle, wrap="word", undo=True)
        draft_frame.grid(row=1, column=0, sticky="nsew")

        right = ttk.Frame(self, padding=(0, 10, 10, 10))
        right.grid(row=0, column=2, sticky="nsew")
        right.rowconfigure(1, weight=1)
        right.rowconfigure(3, minsize=180, weight=0)
        right.columnconfigure(0, weight=1)
        ttk.Label(right, text="Caputre Text Preview", font=("", 10, "bold")).grid(sticky="w", pady=(0, 6))
        preview_frame, self.preview = self._scrolled_text(right, wrap="word", state="disabled")
        preview_frame.grid(row=1, column=0, sticky="nsew")

        prompt_controls = ttk.Frame(right)
        prompt_controls.grid(row=2, column=0, sticky="ew", pady=(12, 8))
        prompt_controls.columnconfigure(0, weight=1)
        prompt_controls.columnconfigure(1, weight=1)
        self.prompt_groups = self._prompt_group_names()
        self.prompt_group_var = tk.StringVar(value=self.prompt_groups[0] if self.prompt_groups else "")
        self.prompt_group_frame = ttk.Frame(prompt_controls)
        self.prompt_group_frame.grid(row=0, column=0, columnspan=2, sticky="ew", pady=(0, 6))
        self._refresh_prompt_groups()

        self.prompt_var = tk.StringVar()
        self.prompt_choice = ttk.Combobox(prompt_controls, textvariable=self.prompt_var, state="readonly")
        self.prompt_choice.grid(
            row=1, column=0, columnspan=2, sticky="ew", pady=(0, 6)
        )
        ttk.Button(prompt_controls, text="Copy Prompt", command=self.copy_prompt).grid(
            row=2, column=0, sticky="ew", padx=(0, 4)
        )
        ttk.Button(prompt_controls, text="Paste AI Result", command=self.paste_ai_result).grid(
            row=2, column=1, sticky="ew", padx=(4, 0)
        )
        self._refresh_prompt_choices()

        workbench = ttk.Frame(right)
        workbench.grid(row=3, column=0, sticky="nsew")
        workbench.rowconfigure(1, weight=1)
        workbench.columnconfigure(0, weight=1)
        header = ttk.Frame(workbench)
        header.grid(row=0, column=0, sticky="ew", pady=(0, 6))
        header.columnconfigure(0, weight=1)
        ttk.Label(header, text="AI Prompt Workbench", font=("", 10, "bold")).grid(row=0, column=0, sticky="w")
        ttk.Button(header, text="Save (AI) Draft", command=self.save_draft_as_fragment).grid(row=0, column=1, sticky="e")
        prompt_frame, self.prompt_box = self._scrolled_text(workbench, height=8, wrap="word", state="disabled")
        prompt_frame.grid(row=1, column=0, sticky="nsew")

        self.status_var = tk.StringVar()
        ttk.Label(self, textvariable=self.status_var, relief="sunken", anchor="w").grid(
            row=1, column=0, columnspan=3, sticky="ew"
        )

    @staticmethod
    def _scrolled_text(parent: tk.Widget, **text_options: object) -> tuple[ttk.Frame, tk.Text]:
        frame = ttk.Frame(parent)
        frame.rowconfigure(0, weight=1)
        frame.columnconfigure(0, weight=1)

        text = tk.Text(frame, **text_options)
        scrollbar = ttk.Scrollbar(frame, orient="vertical", command=text.yview)
        text.configure(yscrollcommand=scrollbar.set)

        text.grid(row=0, column=0, sticky="nsew")
        scrollbar.grid(row=0, column=1, sticky="ns")
        return frame, text

    def choose_workspace(self) -> None:
        initial_dir = workspace_dialog_initial_dir(self.workspace_path, self.last_workspace_path)
        selected = filedialog.askdirectory(title="Choose NoteMan workspace", initialdir=str(initial_dir))
        if selected:
            self.workspace_path = Path(selected)
            self.last_workspace_path = self.workspace_path
            self.workspace_label.configure(text=str(self.workspace_path))
            self._refresh_project_choices()
            self._refresh_note_choices()
            try:
                save_last_workspace(self.workspace_path)
            except OSError:
                self._set_status("Workspace selected. Last workspace preference could not be saved.")
                return
            self._set_status("Workspace selected.")

    def new_note(self) -> None:
        project_name = self.project_var.get().strip()
        note_title = self.note_var.get().strip()
        if not project_name or not note_title:
            messagebox.showinfo("NoteMan", "Project and note title are required.")
            return
        self.current_project = self._load_or_create_project(project_name)
        self.current_note = Note(note_title)
        self.draft.delete("1.0", "end")
        self.draft_loaded_from_note = False
        self._update_preview()
        messagebox.showinfo("NoteMan", f"New note with {note_title} created.")
        self._set_status(f"New note with {note_title} created.")

    def retrieve_note(self) -> None:
        if not self._load_note_for_display(self.note_var.get().strip()):
            self._set_status("Select an exported note from the Note list to retrieve it.")

    def paste_clipboard_text(self) -> None:
        self._ensure_note()
        try:
            text = self.clipboard_get()
        except tk.TclError:
            messagebox.showinfo("NoteMan", "Clipboard has no text.")
            return
        self._add_fragment(self._normalize_captured_text(text), ExtractionMethod.CLIPBOARD_TEXT)
        self.clipboard_clear()

    def clipboard_ocr(self) -> None:
        messagebox.showinfo(
            "NoteMan OCR",
            "Clipboard OCR is not wired yet. It will read an image from the clipboard, run OCR, then capture text with source and page.",
        )

    def export_note(self) -> None:
        if self.workspace_path is None:
            messagebox.showinfo("NoteMan", "Choose a workspace before exporting.")
            return
        self._ensure_note()
        draft_text = self._text_content(self.draft)
        if draft_text and not self._draft_matches_loaded_ai_retrieval(draft_text):
            self._add_fragment(draft_text, ExtractionMethod.MANUAL)
        repo = FileProjectRepository(self.workspace_path)
        note_path = repo.save_note(self.current_project, self.current_note)  # type: ignore[arg-type]
        self._replace_text(self.preview, "", disabled=True)
        self.draft.delete("1.0", "end")
        self.draft_loaded_from_note = False
        self._refresh_project_choices()
        self._refresh_note_choices()
        self._set_status(f"Exported to {note_path}")

    def clear_typed_draft(self) -> None:
        if not self._text_content(self.draft):
            self._set_status("Typed draft is already empty. Use Undo Last Capture to remove preview text.")
            return
        if messagebox.askyesno("NoteMan", "Clear typed draft text? Captured preview fragments will stay."):
            self.draft.delete("1.0", "end")
            self.draft_loaded_from_note = False
            self._set_status("Typed draft cleared.")

    def undo_last_capture(self) -> None:
        if self.current_note is None or not self.current_note.fragments:
            self._set_status("No captured fragments to undo.")
            return
        last = self.current_note.fragments.pop()
        self._update_preview()
        self._set_status(f"Removed last capture from {last.citation_heading()}.")

    def copy_prompt(self) -> None:
        if self.workspace_path is None:
            messagebox.showinfo("NoteMan", "Choose a workspace before copying a prompt.")
            return
        fragment = self._latest_fragment()
        if fragment is None or self.current_project is None or self.current_note is None:
            self._set_status("Capture text first, then copy a prompt.")
            return
        selected_prompt = self._selected_prompt()
        if selected_prompt is None:
            self._set_status("Add a user prompt before copying from the User group.")
            return
        rendered_prompt = render_prompt(selected_prompt, fragment)
        try:
            FileProjectRepository(self.workspace_path).save_prompt_use(
                self.current_project,
                self.current_note,
                fragment,
                selected_prompt.title,
                "user" if selected_prompt.group == "User" else "built_in",
                selected_prompt.body,
                rendered_prompt,
            )
        except (OSError, ValueError) as exc:
            messagebox.showerror("NoteMan", f"The prompt could not be logged and was not copied: {exc}")
            return
        self._replace_text(self.prompt_box, rendered_prompt, disabled=True)
        self.clipboard_clear()
        self.clipboard_append(rendered_prompt)
        self._set_status(f"Copied and logged {selected_prompt.title} prompt for {fragment.citation_heading()}.")

    def paste_ai_result(self) -> None:
        try:
            text = self.clipboard_get().strip()
        except tk.TclError:
            self._set_status("Clipboard has no AI result text.")
            return
        if self._text_content(self.draft):
            self.draft.insert("end", "\n\n")
        self.draft.insert("end", text)
        self.draft.see("end")
        self.draft.focus_set()
        self.draft_loaded_from_note = False
        self._set_status("Appended AI result to Typed / AI Draft. Review it before saving.")

    def add_prompt(self) -> None:
        result = self._prompt_editor_dialog()
        if result is None:
            return
        title, prompt_text, keep_after_closing = result
        if any(prompt.title.casefold() == title.casefold() for prompt in self.prompts):
            messagebox.showinfo("NoteMan", "A prompt with that name already exists.")
            return

        body = f"{title}\n\n{prompt_text}"
        path = ""
        if keep_after_closing:
            try:
                directory = user_prompt_directory()
                directory.mkdir(parents=True, exist_ok=True)
                prompt_path = self._unique_prompt_path(directory, title)
                prompt_path.write_text(f"{title}\nGroup: User\n\n{prompt_text}\n", encoding="utf-8")
                path = str(prompt_path)
            except OSError as exc:
                messagebox.showerror("NoteMan", f"The prompt could not be saved: {exc}")
                return

        prompt = PromptTemplate(title=title, body=body, path=path, group="User")
        self.prompts.append(prompt)
        self._refresh_prompt_groups("User")
        self._refresh_prompt_choices(title)
        status_kind = "user" if keep_after_closing else "temporary"
        self._set_status(f"Added {status_kind} prompt '{title}'.")

    def remove_prompt(self) -> None:
        prompt = self._selected_prompt()
        if prompt is None:
            self._set_status("There are no user prompts to remove.")
            return
        if prompt.group != "User":
            self._set_status("Only user-defined prompts can be removed here.")
            return
        if not messagebox.askyesno("NoteMan", f"Remove the user prompt '{prompt.title}'?"):
            return
        if prompt.path:
            try:
                prompt_path = Path(prompt.path).resolve()
                if prompt_path.parent != user_prompt_directory().resolve():
                    self._set_status("Built-in prompts cannot be removed here.")
                    return
                prompt_path.unlink(missing_ok=True)
            except OSError as exc:
                messagebox.showerror("NoteMan", f"The prompt could not be removed: {exc}")
                return
        self.prompts.remove(prompt)
        self._refresh_prompt_groups()
        self._refresh_prompt_choices()
        self._set_status(f"Removed user prompt '{prompt.title}'.")

    def _prompt_editor_dialog(self) -> tuple[str, str, bool] | None:
        dialog = tk.Toplevel(self)
        dialog.title("Add User Prompt")
        dialog.geometry("560x430")
        dialog.minsize(440, 340)
        dialog.transient(self)
        dialog.grab_set()
        dialog.columnconfigure(0, weight=1)
        dialog.rowconfigure(3, weight=1)

        ttk.Label(dialog, text="Prompt name", font=("", 10, "bold")).grid(
            row=0, column=0, sticky="w", padx=14, pady=(14, 5)
        )
        title_var = tk.StringVar()
        title_entry = ttk.Entry(dialog, textvariable=title_var)
        title_entry.grid(row=1, column=0, sticky="ew", padx=14, pady=(0, 12))
        ttk.Label(dialog, text="Prompt text", font=("", 10, "bold")).grid(
            row=2, column=0, sticky="w", padx=14, pady=(0, 5)
        )
        body_frame, body_box = self._scrolled_text(dialog, wrap="word")
        body_frame.grid(row=3, column=0, sticky="nsew", padx=14)
        persistent_var = tk.BooleanVar(value=True)
        ttk.Checkbutton(dialog, text="Keep after closing NoteMan", variable=persistent_var).grid(
            row=4, column=0, sticky="w", padx=14, pady=10
        )
        buttons = ttk.Frame(dialog)
        buttons.grid(row=5, column=0, sticky="e", padx=14, pady=(0, 14))
        result: list[tuple[str, str, bool]] = []

        def accept() -> None:
            title = title_var.get().strip()
            body = self._text_content(body_box)
            if not title or not body:
                messagebox.showinfo("NoteMan", "Enter both a prompt name and prompt text.", parent=dialog)
                return
            result.append((title, body, persistent_var.get()))
            dialog.destroy()

        ttk.Button(buttons, text="Cancel", command=dialog.destroy).grid(row=0, column=0, padx=(0, 8))
        ttk.Button(buttons, text="Add Prompt", command=accept).grid(row=0, column=1)
        dialog.protocol("WM_DELETE_WINDOW", dialog.destroy)
        title_entry.focus_set()
        self.wait_window(dialog)
        return result[0] if result else None

    def save_draft_as_fragment(self) -> None:
        draft_text = self._text_content(self.draft)
        if not draft_text:
            self._set_status("Typed / AI Draft is empty.")
            return
        if self._draft_matches_loaded_ai_retrieval(draft_text):
            self._set_status("Loaded AI draft text is already saved.")
            return
        if self.workspace_path is None:
            messagebox.showinfo("NoteMan", "Choose a workspace before saving AI draft.")
            return

        self._ensure_note()
        fragment = self._build_fragment(draft_text, ExtractionMethod.AI_DRAFT)
        repo = FileProjectRepository(self.workspace_path)
        corpus_path = repo.save_ai_corpus_entry(
            self.current_project,  # type: ignore[arg-type]
            self.current_note,  # type: ignore[arg-type]
            fragment,
        )
        self.current_note.add_fragment(fragment)  # type: ignore[union-attr]
        self.draft.delete("1.0", "end")
        self.draft_loaded_from_note = False
        self._update_preview()
        self._refresh_project_choices()
        self._refresh_note_choices()
        self._set_status(f"Saved AI draft to {corpus_path}.")

    def change_page(self, delta: int) -> None:
        try:
            page = int(self.locator_var.get().strip())
        except ValueError:
            messagebox.showinfo("NoteMan", "Page must be a number.")
            return
        self.locator_var.set(str(max(1, page + delta)))

    def _add_fragment(self, text: str, method: ExtractionMethod, clear_draft: bool = False) -> None:
        self._ensure_note()
        fragment = self._build_fragment(text, method)
        self.current_note.add_fragment(fragment)  # type: ignore[union-attr]
        if clear_draft:
            self.draft.delete("1.0", "end")
            self.draft_loaded_from_note = False
        self._update_preview()
        self._set_status(f"Captured fragment from {fragment.citation_heading()}.")

    def _build_fragment(self, text: str, method: ExtractionMethod) -> CaptureFragment:
        source_label = self.source_var.get().strip()
        if not source_label or source_label == "Reference...":
            source_label = "Unknown"
        locator_value = self.locator_var.get().strip()
        return CaptureFragment(
            text=text,
            source=Source(source_label),
            locator=Locator(locator_value, LocatorKind.PAGE if locator_value else LocatorKind.NONE),
            method=method,
        )

    def _ensure_note(self) -> None:
        if self.current_project is None or self.current_note is None:
            self.new_note()

    def _latest_fragment(self) -> CaptureFragment | None:
        if self.current_note is None or not self.current_note.fragments:
            return None
        return self.current_note.fragments[-1]

    def _selected_prompt(self) -> PromptTemplate | None:
        selected = self.prompt_var.get()
        prompts = self._prompts_in_selected_group()
        return next((prompt for prompt in prompts if prompt.title == selected), prompts[0] if prompts else None)

    def _prompt_group_names(self) -> list[str]:
        groups = sorted({prompt.group for prompt in self.prompts})
        if "Research" in groups:
            groups.remove("Research")
            groups.insert(0, "Research")
        if "User" not in groups:
            groups.append("User")
        return groups

    def _prompts_in_selected_group(self) -> list[PromptTemplate]:
        group = self.prompt_group_var.get()
        return [prompt for prompt in self.prompts if prompt.group == group]

    def _refresh_prompt_groups(self, preferred_group: str | None = None) -> None:
        for child in self.prompt_group_frame.winfo_children():
            child.destroy()
        self.prompt_groups = self._prompt_group_names()
        selected = preferred_group if preferred_group in self.prompt_groups else self.prompt_groups[0]
        self.prompt_group_var.set(selected)
        for column, group in enumerate(self.prompt_groups):
            self.prompt_group_frame.columnconfigure(column, weight=1)
            ttk.Radiobutton(
                self.prompt_group_frame, text=group, value=group,
                variable=self.prompt_group_var, command=self._refresh_prompt_choices,
            ).grid(row=0, column=column, sticky="w")

    def _refresh_prompt_choices(self, preferred_title: str | None = None) -> None:
        prompt_titles = [prompt.title for prompt in self._prompts_in_selected_group()]
        self.prompt_choice.configure(values=prompt_titles)
        selected = preferred_title if preferred_title in prompt_titles else (prompt_titles[0] if prompt_titles else "")
        self.prompt_var.set(selected)

    @staticmethod
    def _unique_prompt_path(directory: Path, title: str) -> Path:
        safe_title = "".join(character if character.isalnum() or character in " -_" else "-" for character in title).strip()
        safe_title = safe_title or "user-prompt"
        path = directory / f"{safe_title}.txt"
        suffix = 2
        while path.exists():
            path = directory / f"{safe_title}-{suffix}.txt"
            suffix += 1
        return path

    def _update_preview(self) -> None:
        latest_capture = next(
            (
                fragment
                for fragment in reversed(self.current_note.fragments if self.current_note else [])
                if fragment.method != ExtractionMethod.AI_DRAFT
            ),
            None,
        )
        content = "" if latest_capture is None else "\n".join(render_fragment(latest_capture)).rstrip() + "\n"
        self._replace_text(self.preview, content, disabled=True)
        self.preview.see("1.0")

    def _render_note_by_ai_method(self, include_ai: bool) -> str:
        if self.current_note is None:
            return ""
        lines = [f"# {self.current_note.title}", ""]
        for fragment in self.current_note.fragments:
            is_ai = fragment.method == ExtractionMethod.AI_DRAFT
            if is_ai == include_ai:
                lines.extend(render_fragment(fragment))
        return "\n".join(lines).rstrip() + "\n"

    def _refresh_project_choices(self) -> None:
        if self.workspace_path is None:
            return
        projects = FileProjectRepository(self.workspace_path).list_project_names()
        self.project_choice.configure(values=projects)

    def _refresh_note_choices(self, *_args: object) -> None:
        if self.workspace_path is None:
            return
        summaries = FileProjectRepository(self.workspace_path).list_note_summaries(self.project_var.get().strip())
        title_counts: dict[str, int] = {}
        for summary in summaries:
            title_counts[summary.title] = title_counts.get(summary.title, 0) + 1

        self.note_choice_index = {}
        values: list[str] = []
        for summary in summaries:
            display = summary.title if title_counts[summary.title] == 1 else f"{summary.title} [{summary.id}]"
            self.note_choice_index[display] = summary.id
            values.append(display)
        self.note_choice.configure(values=values)

    def _load_selected_note(self, *_args: object) -> None:
        self._load_note_for_display(self.note_var.get().strip())

    def _load_note_for_display(self, display: str) -> bool:
        if self.workspace_path is None:
            return False
        note_id = self.note_choice_index.get(display)
        if note_id is None:
            return False
        project_name = self.project_var.get().strip()
        repo = FileProjectRepository(self.workspace_path)
        note = repo.load_note(project_name, note_id)
        if note is None:
            self._set_status("Selected note could not be loaded.")
            return False
        self.current_project = self._load_or_create_project(project_name)
        self.current_note = note
        self.note_var.set(display)
        self._replace_text(self.draft, render_note_markdown(note))
        self.draft.mark_set("insert", "1.0")
        self.draft.see("1.0")
        self.draft_loaded_from_note = True
        self._update_preview()
        self._set_status("Retrieved existing note into Typed / AI Draft.")
        return True

    def _draft_matches_loaded_ai_retrieval(self, draft_text: str) -> bool:
        if not self.draft_loaded_from_note:
            return False
        return draft_text == render_note_markdown(self.current_note).strip()  # type: ignore[arg-type]

    def _load_or_create_project(self, project_name: str) -> Project:
        if self.workspace_path is None:
            return Project(project_name)
        return FileProjectRepository(self.workspace_path).load_project(project_name) or Project(project_name)

    def _replace_text(self, widget: tk.Text, text: str, disabled: bool = False) -> None:
        widget.configure(state="normal")
        widget.delete("1.0", "end")
        widget.insert("1.0", text)
        if disabled:
            widget.configure(state="disabled")

    def _set_status(self, message: str) -> None:
        self.status_var.set(message)

    @staticmethod
    def _text_content(widget: tk.Text) -> str:
        return widget.get("1.0", "end").strip()

    @staticmethod
    def _normalize_captured_text(value: str) -> str:
        return value.replace("-\r\n", "").replace("-\n", "").replace("\r\n", " ").replace("\n", " ").strip()


def main() -> None:
    app = NoteManDesktopApp()
    app.mainloop()


if __name__ == "__main__":
    main()
