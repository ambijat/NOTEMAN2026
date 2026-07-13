"""Capture a local screenshot or image into a NoteMan WCS workspace."""

from __future__ import annotations

import argparse
from pathlib import Path

from noteman_wcs.extraction import OllamaVisionExtractor, TesseractImageExtractor
from noteman_wcs.image_capture import capture_image_fragment
from noteman_wcs.storage import FileProjectRepository


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Capture one local image as a source-aware NoteMan fragment.")
    parser.add_argument("--workspace", required=True, type=Path, help="Workspace folder to write into.")
    parser.add_argument("--project", required=True, help="Project name, such as 'Thesis Notes'.")
    parser.add_argument("--note", required=True, help="Note title, such as 'Chapter One'.")
    parser.add_argument("--source", required=True, help="Source label, such as 'Book Screenshot'.")
    parser.add_argument("--locator", default="", help="Page, timestamp, or other locator. Page numbers are default.")
    parser.add_argument("--image", required=True, type=Path, help="Local screenshot or image file.")
    parser.add_argument("--mode", choices=["tesseract", "ollama"], default="tesseract")
    parser.add_argument("--language", default="eng", help="Tesseract language code.")
    parser.add_argument("--model", default="llava", help="Ollama vision model name.")
    parser.add_argument("--ollama-endpoint", default="http://localhost:11434/api/generate")
    return parser


def main(argv: list[str] | None = None) -> None:
    args = build_parser().parse_args(argv)
    extractor = (
        OllamaVisionExtractor(model=args.model, endpoint=args.ollama_endpoint)
        if args.mode == "ollama"
        else TesseractImageExtractor(language=args.language)
    )
    result = capture_image_fragment(
        repository=FileProjectRepository(args.workspace),
        project_name=args.project,
        note_title=args.note,
        source_label=args.source,
        locator_value=args.locator,
        image_path=args.image,
        extractor=extractor,
    )
    print(f"Saved note: {result.note_path}")
    print(f"Copied asset: {result.asset_path}")
    print(f"Captured: {result.fragment.citation_heading()}")


if __name__ == "__main__":
    main()
