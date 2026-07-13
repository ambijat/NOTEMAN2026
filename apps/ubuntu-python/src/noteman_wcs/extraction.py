"""Local image extraction adapters for NoteMan WCS."""

from __future__ import annotations

import base64
import json
import subprocess
from dataclasses import dataclass
from pathlib import Path
from typing import Protocol
from urllib import request


class ImageExtractor(Protocol):
    def extract(self, image_path: Path) -> str:
        """Return text extracted or interpreted from a local image."""


@dataclass(frozen=True)
class StaticTextExtractor:
    """Test helper and development fallback."""

    text: str

    def extract(self, image_path: Path) -> str:
        return self.text


@dataclass(frozen=True)
class TesseractImageExtractor:
    language: str = "eng"
    command: str = "tesseract"

    def extract(self, image_path: Path) -> str:
        completed = subprocess.run(
            [self.command, str(image_path), "stdout", "-l", self.language],
            capture_output=True,
            check=False,
            text=True,
        )
        if completed.returncode != 0:
            detail = completed.stderr.strip() or "Tesseract failed without an error message."
            raise RuntimeError(detail)
        return completed.stdout


@dataclass(frozen=True)
class OllamaVisionExtractor:
    model: str = "llava"
    endpoint: str = "http://localhost:11434/api/generate"
    prompt: str = (
        "Read this screenshot or page image locally. Extract readable text, preserve source wording where clear, "
        "and summarize only where the image is not exact text. Return plain text only."
    )

    def extract(self, image_path: Path) -> str:
        encoded_image = base64.b64encode(Path(image_path).read_bytes()).decode("ascii")
        payload = {
            "model": self.model,
            "prompt": self.prompt,
            "images": [encoded_image],
            "stream": False,
        }
        body = json.dumps(payload).encode("utf-8")
        http_request = request.Request(
            self.endpoint,
            data=body,
            headers={"Content-Type": "application/json"},
            method="POST",
        )
        with request.urlopen(http_request, timeout=120) as response:
            value = json.loads(response.read().decode("utf-8"))
        return str(value.get("response", "")).strip()


def normalize_extracted_text(value: str) -> str:
    return value.replace("-\r\n", "").replace("-\n", "").replace("\r\n", " ").replace("\n", " ").strip()
