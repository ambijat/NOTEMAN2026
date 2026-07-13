import json
import tempfile
import unittest
from pathlib import Path

from noteman_wcs.extraction import StaticTextExtractor
from noteman_wcs.image_capture import capture_image_fragment
from noteman_wcs.storage import FileProjectRepository


class ScreenshotCaptureTests(unittest.TestCase):
    def test_capture_image_writes_asset_markdown_and_json(self):
        with tempfile.TemporaryDirectory() as tmp:
            workspace = Path(tmp) / "workspace"
            image = Path(tmp) / "page12.png"
            image.write_bytes(b"fake image bytes")

            result = capture_image_fragment(
                repository=FileProjectRepository(workspace),
                project_name="Thesis Notes",
                note_title="Chapter One",
                source_label="Research Book",
                locator_value="12",
                image_path=image,
                extractor=StaticTextExtractor("Extracted\ntext."),
            )

            self.assertTrue(result.asset_path.exists())
            self.assertEqual(b"fake image bytes", result.asset_path.read_bytes())

            markdown = result.note_path.read_text(encoding="utf-8")
            self.assertIn("# Chapter One", markdown)
            self.assertIn("## Research Book, p. 12", markdown)
            self.assertIn("Extracted text.", markdown)

            sidecar = json.loads(result.note_path.with_suffix(".json").read_text(encoding="utf-8"))
            fragment = sidecar["fragments"][0]
            self.assertEqual("image_ocr", fragment["method"])
            self.assertEqual(result.fragment.asset_id, fragment["asset_id"])


if __name__ == "__main__":
    unittest.main()
