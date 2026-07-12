import tempfile
import json
import hashlib
import unittest
from pathlib import Path

from noteman_wcs import (
    CaptureFragment,
    ExtractionMethod,
    FileProjectRepository,
    Locator,
    LocatorKind,
    Note,
    Project,
    Source,
    SourceType,
)


class DomainTests(unittest.TestCase):
    def test_fragment_requires_text_or_asset(self):
        with self.assertRaises(ValueError):
            CaptureFragment(text="", source=Source("Unknown"))

    def test_note_exports_source_aware_markdown(self):
        project = Project("Thesis Notes")
        note = Note("Chapter One")
        note.add_fragment(
            CaptureFragment(
                text="A source-aware note.",
                source=Source("Research Book", SourceType.BOOK),
                locator=Locator("12", LocatorKind.PAGE),
            )
        )

        with tempfile.TemporaryDirectory() as tmp:
            repo = FileProjectRepository(Path(tmp))
            note_path = repo.save_note(project, note)
            content = note_path.read_text(encoding="utf-8")

        self.assertIn("# Chapter One", content)
        self.assertIn("## Research Book, p. 12", content)
        self.assertIn("A source-aware note.", content)

    def test_project_creates_ai_corpus_folder(self):
        project = Project("Thesis Notes")

        with tempfile.TemporaryDirectory() as tmp:
            repo = FileProjectRepository(Path(tmp))
            project_path = repo.create_project(project)

            self.assertTrue((project_path / "assets").is_dir())
            self.assertTrue((project_path / "ai_corpus").is_dir())
            self.assertTrue((project_path / "notes").is_dir())
            self.assertTrue((project_path / "prompts" / "snapshots").is_dir())

    def test_prompt_use_saves_exact_snapshot_and_jsonl_provenance(self):
        project = Project("Thesis Notes")
        note = Note("Chapter One")
        fragment = CaptureFragment(
            text="Exact source passage.",
            source=Source("Research Book", SourceType.BOOK),
            locator=Locator("12", LocatorKind.PAGE),
        )
        note.add_fragment(fragment)
        template = "Summarize {fragment_text}"
        rendered = "Summarize Exact source passage."

        with tempfile.TemporaryDirectory() as tmp:
            repo = FileProjectRepository(Path(tmp))
            snapshot_path = repo.save_prompt_use(
                project, note, fragment, "Summary", "built_in", template, rendered
            )
            entry = json.loads(
                (snapshot_path.parent.parent / "usage.jsonl").read_text(encoding="utf-8").strip()
            )

            self.assertEqual(rendered, snapshot_path.read_text(encoding="utf-8"))
            self.assertEqual("built_in", entry["template_origin"])
            self.assertEqual(note.id, entry["note_id"])
            self.assertEqual(fragment.id, entry["fragment_id"])
            self.assertEqual("snapshots/" + snapshot_path.name, entry["snapshot"])
            self.assertEqual(hashlib.sha256(template.encode()).hexdigest(), entry["template_sha256"])
            self.assertEqual(hashlib.sha256(rendered.encode()).hexdigest(), entry["rendered_sha256"])

    def test_project_names_list_only_project_folders(self):
        with tempfile.TemporaryDirectory() as tmp:
            workspace = Path(tmp)
            repo = FileProjectRepository(workspace)
            repo.create_project(Project("Zettel Notes"))
            repo.create_project(Project("Archive"))
            (workspace / "loose-assets").mkdir()

            self.assertEqual(["Archive", "Zettel Notes"], repo.list_project_names())

    def test_note_summaries_list_and_load_saved_notes(self):
        project = Project("Thesis Notes")
        note = Note("Chapter One")
        note.add_fragment(
            CaptureFragment(
                text="Loaded text.",
                source=Source("Research Book", SourceType.BOOK),
                locator=Locator("12", LocatorKind.PAGE),
            )
        )

        with tempfile.TemporaryDirectory() as tmp:
            repo = FileProjectRepository(Path(tmp))
            repo.save_note(project, note)

            summaries = repo.list_note_summaries(project.name)
            loaded = repo.load_note(project.name, summaries[0].id)

        self.assertEqual([(note.id, "Chapter One")], [(summary.id, summary.title) for summary in summaries])
        self.assertIsNotNone(loaded)
        self.assertEqual(note.id, loaded.id)  # type: ignore[union-attr]
        self.assertEqual("Loaded text.", loaded.fragments[0].text)  # type: ignore[union-attr]

    def test_ai_draft_saves_to_corpus_markdown_and_json(self):
        project = Project("Thesis Notes")
        note = Note("Chapter One")
        fragment = CaptureFragment(
            text="AI-generated reviewed draft.",
            source=Source("Research Book", SourceType.BOOK),
            locator=Locator("12", LocatorKind.PAGE),
            method=ExtractionMethod.AI_DRAFT,
        )

        with tempfile.TemporaryDirectory() as tmp:
            repo = FileProjectRepository(Path(tmp))
            corpus_path = repo.save_ai_corpus_entry(project, note, fragment)
            content = corpus_path.read_text(encoding="utf-8")
            sidecar = json.loads(corpus_path.with_suffix(".json").read_text(encoding="utf-8"))

        self.assertEqual(corpus_path.parent.name, "ai_corpus")
        self.assertIn("# AI Draft Corpus Entry", content)
        self.assertIn("Source: Research Book, p. 12", content)
        self.assertIn("AI-generated reviewed draft.", content)
        self.assertEqual(note.id, sidecar["note_id"])
        self.assertEqual("ai_draft", sidecar["fragment"]["method"])


if __name__ == "__main__":
    unittest.main()
