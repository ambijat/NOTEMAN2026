import json
import tempfile
import unittest
from pathlib import Path

from noteman_wcs.desktop_app import load_last_workspace, save_last_workspace, workspace_dialog_initial_dir


class DesktopPreferenceTests(unittest.TestCase):
    def test_last_workspace_round_trips_through_config_file(self):
        with tempfile.TemporaryDirectory() as tmp:
            config_path = Path(tmp) / "desktop_app.json"
            workspace = Path(tmp) / "workspace"
            workspace.mkdir()

            save_last_workspace(workspace, config_path)

            self.assertEqual(workspace.resolve(), load_last_workspace(config_path))

    def test_invalid_preference_file_returns_none(self):
        with tempfile.TemporaryDirectory() as tmp:
            config_path = Path(tmp) / "desktop_app.json"
            config_path.write_text("{not json", encoding="utf-8")

            self.assertIsNone(load_last_workspace(config_path))

    def test_dialog_initial_dir_prefers_current_then_last_then_home(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            current = root / "current"
            last = root / "last"
            current.mkdir()
            last.mkdir()

            self.assertEqual(current, workspace_dialog_initial_dir(current, last))
            self.assertEqual(last, workspace_dialog_initial_dir(root / "missing", last))

    def test_dialog_initial_dir_uses_parent_for_missing_saved_workspace(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            missing_workspace = root / "old-workspace"

            self.assertEqual(root, workspace_dialog_initial_dir(None, missing_workspace))

    def test_non_string_workspace_preference_is_ignored(self):
        with tempfile.TemporaryDirectory() as tmp:
            config_path = Path(tmp) / "desktop_app.json"
            config_path.write_text(json.dumps({"last_workspace": 12}), encoding="utf-8")

            self.assertIsNone(load_last_workspace(config_path))


if __name__ == "__main__":
    unittest.main()
