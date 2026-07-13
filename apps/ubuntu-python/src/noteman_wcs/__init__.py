"""Workspace Capture System core for NoteMan."""

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
    Workspace,
)
from .image_capture import ImageCaptureResult, capture_image_fragment
from .storage import FileProjectRepository

__all__ = [
    "Asset",
    "CaptureFragment",
    "ExtractionMethod",
    "FileProjectRepository",
    "ImageCaptureResult",
    "Locator",
    "LocatorKind",
    "Note",
    "Project",
    "Source",
    "SourceType",
    "Workspace",
    "capture_image_fragment",
]
