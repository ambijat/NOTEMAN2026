import unittest

from noteman_wcs import CaptureFragment, Locator, LocatorKind, Source
from noteman_wcs.prompts import PromptTemplate, load_prompt_templates, render_prompt


class PromptTests(unittest.TestCase):
    def test_packaged_prompts_load(self):
        prompts = load_prompt_templates()

        self.assertGreaterEqual(len(prompts), 28)
        self.assertTrue(any(prompt.title == "Clean OCR" for prompt in prompts))
        self.assertTrue(any(prompt.title == "Classroom Pedagogy Notes" for prompt in prompts))
        self.assertTrue(any(prompt.title == "Remove Every AI Tell" for prompt in prompts))
        self.assertTrue(any(prompt.group == "Human Writing" for prompt in prompts))
        self.assertFalse(any("Group: Human Writing" in prompt.body for prompt in prompts))

    def test_prompt_rendering_preserves_source_and_locator(self):
        fragment = CaptureFragment(
            text="Captured text.",
            source=Source("Research Book"),
            locator=Locator("12", LocatorKind.PAGE),
        )
        prompt = PromptTemplate(
            title="Test",
            body="Source: {source}\nLocator: {locator}\nText: {fragment_text}",
        )

        rendered = render_prompt(prompt, fragment)

        self.assertIn("Source: Research Book", rendered)
        self.assertIn("Locator: p. 12", rendered)
        self.assertIn("Text: Captured text.", rendered)


if __name__ == "__main__":
    unittest.main()
