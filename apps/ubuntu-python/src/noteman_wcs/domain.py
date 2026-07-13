"""Domain model for NoteMan Workspace Capture System."""

from __future__ import annotations

from dataclasses import dataclass, field
from datetime import datetime, timezone
from enum import StrEnum
from pathlib import Path
from uuid import uuid4


def new_id(prefix: str) -> str:
    return f"{prefix}-{uuid4().hex[:12]}"


def utc_now() -> str:
    return datetime.now(timezone.utc).isoformat(timespec="seconds")


class SourceType(StrEnum):
    UNKNOWN = "unknown"
    BOOK = "book"
    ARTICLE = "article"
    PDF = "pdf"
    WEBPAGE = "webpage"
    LECTURE = "lecture"
    IMAGE = "image"
    CLIPBOARD = "clipboard"


class LocatorKind(StrEnum):
    NONE = "none"
    PAGE = "page"
    PAGE_RANGE = "page_range"
    TIMESTAMP = "timestamp"
    SECTION = "section"
    URL = "url"
    FILE = "file"


class ExtractionMethod(StrEnum):
    MANUAL = "manual"
    AI_DRAFT = "ai_draft"
    CLIPBOARD_TEXT = "clipboard_text"
    CLIPBOARD_OCR = "clipboard_ocr"
    IMAGE_OCR = "image_ocr"
    PDF_TEXT = "pdf_text"


@dataclass(frozen=True)
class Workspace:
    path: Path


@dataclass
class Project:
    name: str
    id: str = field(default_factory=lambda: new_id("project"))
    created_at: str = field(default_factory=utc_now)


@dataclass
class Note:
    title: str
    id: str = field(default_factory=lambda: new_id("note"))
    created_at: str = field(default_factory=utc_now)
    fragments: list["CaptureFragment"] = field(default_factory=list)

    def add_fragment(self, fragment: "CaptureFragment") -> None:
        self.fragments.append(fragment)


@dataclass(frozen=True)
class Source:
    label: str
    type: SourceType = SourceType.UNKNOWN


@dataclass(frozen=True)
class Locator:
    value: str = ""
    kind: LocatorKind = LocatorKind.NONE

    def display(self) -> str:
        if self.kind == LocatorKind.NONE or not self.value:
            return ""
        if self.kind == LocatorKind.PAGE:
            return f"p. {self.value}"
        return f"{self.kind.value}: {self.value}"


@dataclass(frozen=True)
class Asset:
    path: Path
    media_type: str
    id: str = field(default_factory=lambda: new_id("asset"))


@dataclass
class CaptureFragment:
    text: str
    source: Source
    locator: Locator = field(default_factory=Locator)
    method: ExtractionMethod = ExtractionMethod.MANUAL
    asset_id: str | None = None
    id: str = field(default_factory=lambda: new_id("fragment"))
    created_at: str = field(default_factory=utc_now)

    def __post_init__(self) -> None:
        if not self.text.strip() and self.asset_id is None:
            raise ValueError("A fragment needs text or a recoverable asset reference.")

    def citation_heading(self) -> str:
        locator = self.locator.display()
        if locator:
            return f"{self.source.label}, {locator}"
        return self.source.label
