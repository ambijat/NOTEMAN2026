"""Image and screenshot capture workflow for NoteMan WCS."""

from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path

from .domain import CaptureFragment, ExtractionMethod, Locator, LocatorKind, Note, Project, Source, SourceType
from .extraction import ImageExtractor, normalize_extracted_text
from .storage import FileProjectRepository


@dataclass(frozen=True)
class ImageCaptureResult:
    note_path: Path
    asset_path: Path
    fragment: CaptureFragment


def capture_image_fragment(
    *,
    repository: FileProjectRepository,
    project_name: str,
    note_title: str,
    source_label: str,
    locator_value: str,
    image_path: Path,
    extractor: ImageExtractor,
) -> ImageCaptureResult:
    project_name = project_name.strip()
    note_title = note_title.strip()
    image_path = Path(image_path)
    if not project_name:
        raise ValueError("Project name is required.")
    if not note_title:
        raise ValueError("Note title is required.")
    if not image_path.is_file():
        raise FileNotFoundError(image_path)

    project = Project(project_name)
    note = Note(note_title)
    asset = repository.copy_asset(project, image_path)
    text = normalize_extracted_text(extractor.extract(image_path))
    fragment = CaptureFragment(
        text=text,
        source=Source(source_label.strip() or "Unknown", SourceType.IMAGE),
        locator=Locator(locator_value.strip(), LocatorKind.PAGE if locator_value.strip() else LocatorKind.NONE),
        method=ExtractionMethod.IMAGE_OCR,
        asset_id=asset.id,
    )
    note.add_fragment(fragment)
    note_path = repository.save_note(project, note)
    asset_path = repository.workspace / project.name / asset.path
    return ImageCaptureResult(note_path=note_path, asset_path=asset_path, fragment=fragment)
