"""File-based storage for NoteMan WCS projects."""

from __future__ import annotations

import json
import hashlib
import mimetypes
import shutil
from dataclasses import asdict, dataclass
from datetime import datetime, timezone
from pathlib import Path
from uuid import uuid4

from .domain import (
    Asset,
    CaptureFragment,
    ExtractionMethod,
    Locator,
    LocatorKind,
    Note,
    Project,
    Source,
    SourceType,
    new_id,
)


@dataclass(frozen=True)
class NoteSummary:
    id: str
    title: str


class FileProjectRepository:
    """Persist projects and notes as folders, JSON metadata, and Markdown."""

    def __init__(self, workspace: Path):
        self.workspace = Path(workspace)

    def create_project(self, project: Project) -> Path:
        project_path = self._project_path(project.name)
        project_path.mkdir(parents=True, exist_ok=True)
        (project_path / "assets").mkdir(exist_ok=True)
        (project_path / "ai_corpus").mkdir(exist_ok=True)
        (project_path / "notes").mkdir(exist_ok=True)
        (project_path / "prompts" / "snapshots").mkdir(parents=True, exist_ok=True)
        self._write_json(project_path / "project.json", asdict(project))
        return project_path

    def save_prompt_use(
        self,
        project: Project,
        note: Note,
        fragment: CaptureFragment,
        template_title: str,
        template_origin: str,
        template_text: str,
        rendered_prompt: str,
    ) -> Path:
        if not template_title.strip() or not template_origin.strip() or not rendered_prompt.strip():
            raise ValueError("Prompt title, origin, and rendered text are required.")

        project_path = self.create_project(project)
        prompts_path = project_path / "prompts"
        snapshots_path = prompts_path / "snapshots"
        used_at = datetime.now(timezone.utc)
        prompt_use_id = f"prompt-use-{used_at:%Y%m%dT%H%M%S%fZ}-{uuid4().hex}"
        snapshot_path = snapshots_path / f"{prompt_use_id}.txt"
        snapshot_path.write_text(rendered_prompt, encoding="utf-8")

        entry = {
            "id": prompt_use_id,
            "used_at": used_at.isoformat(),
            "template_title": template_title,
            "template_origin": template_origin,
            "template_sha256": _sha256(template_text),
            "rendered_sha256": _sha256(rendered_prompt),
            "project_id": project.id,
            "project_name": project.name,
            "note_id": note.id,
            "note_title": note.title,
            "fragment_id": fragment.id,
            "source": fragment.source.label,
            "locator_kind": fragment.locator.kind.value,
            "locator_value": fragment.locator.value,
            "snapshot": f"snapshots/{snapshot_path.name}",
        }
        with (prompts_path / "usage.jsonl").open("a", encoding="utf-8", newline="\n") as usage_log:
            usage_log.write(json.dumps(entry, separators=(",", ":")) + "\n")
        return snapshot_path

    def save_note(self, project: Project, note: Note) -> Path:
        project_path = self.create_project(project)
        note_path = project_path / "notes" / f"{note.id}.md"
        note_path.write_text(render_note_markdown(note), encoding="utf-8")
        self._write_json(project_path / "notes" / f"{note.id}.json", asdict(note))
        return note_path

    def list_project_names(self) -> list[str]:
        if not self.workspace.is_dir():
            return []
        return sorted(
            (
                path.name
                for path in self.workspace.iterdir()
                if path.is_dir() and (path / "project.json").is_file()
            ),
            key=str.casefold,
        )

    def load_project(self, project_name: str) -> Project | None:
        project_path = self._project_path(project_name) / "project.json"
        try:
            value = json.loads(project_path.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError):
            return None
        if not isinstance(value, dict):
            return None
        try:
            return Project(
                name=str(value["name"]),
                id=str(value["id"]),
                created_at=str(value["created_at"]),
            )
        except KeyError:
            return None

    def list_note_summaries(self, project_name: str) -> list[NoteSummary]:
        notes_path = self._project_path(project_name) / "notes"
        if not notes_path.is_dir():
            return []
        summaries: list[NoteSummary] = []
        for path in notes_path.glob("*.json"):
            try:
                value = json.loads(path.read_text(encoding="utf-8"))
            except (OSError, json.JSONDecodeError):
                continue
            if isinstance(value, dict) and isinstance(value.get("id"), str) and isinstance(value.get("title"), str):
                summaries.append(NoteSummary(id=value["id"], title=value["title"]))
        return sorted(summaries, key=lambda summary: (summary.title.casefold(), summary.id))

    def load_note(self, project_name: str, note_id: str) -> Note | None:
        note_path = self._project_path(project_name) / "notes" / f"{note_id}.json"
        try:
            value = json.loads(note_path.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError):
            return None
        if not isinstance(value, dict):
            return None
        try:
            return _note_from_dict(value)
        except (KeyError, TypeError, ValueError):
            return None

    def save_ai_corpus_entry(self, project: Project, note: Note, fragment: CaptureFragment) -> Path:
        project_path = self.create_project(project)
        corpus_path = project_path / "ai_corpus"
        entry_name = f"{note.id}-{fragment.id}"
        markdown_path = corpus_path / f"{entry_name}.md"
        json_path = corpus_path / f"{entry_name}.json"

        markdown_path.write_text(render_ai_corpus_markdown(note, fragment), encoding="utf-8")
        self._write_json(
            json_path,
            {
                "id": entry_name,
                "note_id": note.id,
                "note_title": note.title,
                "fragment": asdict(fragment),
                "created_at": fragment.created_at,
            },
        )
        return markdown_path

    def copy_asset(self, project: Project, source_path: Path) -> Asset:
        project_path = self.create_project(project)
        source_path = Path(source_path)
        media_type = mimetypes.guess_type(source_path.name)[0] or "application/octet-stream"
        asset_id = new_id("asset")
        asset = Asset(
            path=Path("assets") / f"{asset_id}{source_path.suffix.lower()}",
            media_type=media_type,
            id=asset_id,
        )
        destination = project_path / asset.path
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source_path, destination)
        return asset

    @staticmethod
    def _write_json(path: Path, value: dict) -> None:
        path.write_text(json.dumps(value, indent=2, default=str), encoding="utf-8")

    def _project_path(self, project_name: str) -> Path:
        return self.workspace / project_name


def _sha256(value: str) -> str:
    return hashlib.sha256(value.encode("utf-8")).hexdigest()


def render_note_markdown(note: Note) -> str:
    lines = [f"# {note.title}", ""]
    for fragment in note.fragments:
        lines.extend(render_fragment(fragment))
    return "\n".join(lines).rstrip() + "\n"


def render_fragment(fragment: CaptureFragment) -> list[str]:
    return [
        f"## {fragment.citation_heading()}",
        "",
        fragment.text.strip(),
        "",
    ]


def render_ai_corpus_markdown(note: Note, fragment: CaptureFragment) -> str:
    lines = [
        "# AI Draft Corpus Entry",
        "",
        f"Note: {note.title}",
        f"Note ID: {note.id}",
        f"Fragment ID: {fragment.id}",
        f"Source: {fragment.citation_heading()}",
        f"Method: {fragment.method.value}",
        "",
        "## Draft Text",
        "",
        fragment.text.strip(),
        "",
    ]
    return "\n".join(lines)


def _note_from_dict(value: dict) -> Note:
    note = Note(
        title=str(value["title"]),
        id=str(value["id"]),
        created_at=str(value["created_at"]),
    )
    fragments = value.get("fragments", [])
    if not isinstance(fragments, list):
        raise ValueError("Note fragments must be a list.")
    note.fragments.extend(_fragment_from_dict(fragment) for fragment in fragments if isinstance(fragment, dict))
    return note


def _fragment_from_dict(value: dict) -> CaptureFragment:
    source = value["source"]
    locator = value.get("locator", {})
    if not isinstance(source, dict) or not isinstance(locator, dict):
        raise ValueError("Fragment source and locator must be objects.")
    return CaptureFragment(
        text=str(value["text"]),
        source=Source(
            label=str(source["label"]),
            type=SourceType(str(source.get("type", SourceType.UNKNOWN))),
        ),
        locator=Locator(
            value=str(locator.get("value", "")),
            kind=LocatorKind(str(locator.get("kind", LocatorKind.NONE))),
        ),
        method=ExtractionMethod(str(value.get("method", ExtractionMethod.MANUAL))),
        asset_id=value.get("asset_id"),
        id=str(value["id"]),
        created_at=str(value["created_at"]),
    )
