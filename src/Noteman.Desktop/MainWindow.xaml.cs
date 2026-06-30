using System.IO;
using System.Windows;
using Microsoft.Win32;
using Noteman.Core.Models;
using Noteman.Core.Storage;

namespace Noteman.Desktop;

public partial class MainWindow : Window
{
    private string? workspacePath;
    private Project? currentProject;
    private Note? currentNote;
    private readonly List<PromptTemplate> prompts = [];

    public MainWindow()
    {
        InitializeComponent();
        LoadPrompts();
    }

    private void ChooseWorkspace_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Choose NoteMan workspace"
        };

        if (dialog.ShowDialog() == true)
        {
            workspacePath = dialog.FolderName;
            WorkspacePathText.Text = workspacePath;
            StatusText.Text = "Workspace selected.";
        }
    }

    private void NewNote_Click(object sender, RoutedEventArgs e)
    {
        var projectName = Clean(ProjectNameBox.Text);
        var noteTitle = Clean(NoteTitleBox.Text);
        if (projectName.Length == 0 || noteTitle.Length == 0)
        {
            MessageBox.Show("Project and note title are required.", "NoteMan");
            return;
        }

        currentProject = Project.Create(projectName);
        currentNote = Note.Create(noteTitle);
        DraftBox.Clear();
        UpdatePreview();
        StatusText.Text = "New note ready.";
    }

    private void PasteClipboardText_Click(object sender, RoutedEventArgs e)
    {
        EnsureNote();
        if (!Clipboard.ContainsText())
        {
            MessageBox.Show("Clipboard has no text.", "NoteMan");
            return;
        }

        var text = NormalizeCapturedText(Clipboard.GetText());
        AddFragment(text, ExtractionMethods.ClipboardText);
        Clipboard.Clear();
    }

    private void ClipboardOcr_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Clipboard OCR is not wired yet. It will read an image from the Windows clipboard, run OCR, then capture the text with the current source and page.",
            "NoteMan OCR");
    }

    private void ExportNote_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureWorkspace())
        {
            return;
        }

        EnsureNote();

        if (!string.IsNullOrWhiteSpace(DraftBox.Text))
        {
            AddFragment(DraftBox.Text, ExtractionMethods.Manual, clearDraft: true);
        }

        var repository = new FileProjectRepository(workspacePath!);
        var notePath = repository.SaveNote(currentProject!, currentNote!);
        StatusText.Text = $"Exported to {notePath}";
    }

    private void ClearTypedDraft_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(DraftBox.Text))
        {
            StatusText.Text = "Typed draft is already empty. Use Undo Last Capture to remove preview text.";
            return;
        }

        var answer = MessageBox.Show(
            "Clear typed draft text? Captured preview fragments will stay.",
            "NoteMan",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (answer != MessageBoxResult.Yes)
        {
            return;
        }

        DraftBox.Clear();
        StatusText.Text = "Typed draft cleared.";
    }

    private void UndoLastCapture_Click(object sender, RoutedEventArgs e)
    {
        if (currentNote is null || currentNote.Fragments.Count == 0)
        {
            StatusText.Text = "No captured fragments to undo.";
            return;
        }

        var last = currentNote.Fragments[^1];
        currentNote.Fragments.RemoveAt(currentNote.Fragments.Count - 1);
        UpdatePreview();
        StatusText.Text = $"Removed last capture from {last.CitationHeading()}.";
    }

    private void PreviousPage_Click(object sender, RoutedEventArgs e) => ChangePage(-1);

    private void NextPage_Click(object sender, RoutedEventArgs e) => ChangePage(1);

    private void CopyPrompt_Click(object sender, RoutedEventArgs e)
    {
        var fragment = LatestFragment();
        if (fragment is null)
        {
            StatusText.Text = "Capture text first, then copy a prompt.";
            return;
        }

        var prompt = BuildPrompt(SelectedPrompt(), fragment);
        PromptBox.Text = prompt;
        Clipboard.SetText(prompt);
        StatusText.Text = $"Copied {SelectedPrompt().Title} prompt for {fragment.CitationHeading()}.";
    }

    private void PasteAiResult_Click(object sender, RoutedEventArgs e)
    {
        if (!Clipboard.ContainsText())
        {
            StatusText.Text = "Clipboard has no AI result text.";
            return;
        }

        DraftBox.Text = Clipboard.GetText().Trim();
        StatusText.Text = "Pasted AI result into Typed / AI Draft. Review it before saving.";
    }

    private void SaveDraftAsFragment_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(DraftBox.Text))
        {
            StatusText.Text = "Typed / AI Draft is empty.";
            return;
        }

        AddFragment(DraftBox.Text, ExtractionMethods.Manual, clearDraft: true);
    }

    private void AddFragment(string text, string method, bool clearDraft = false)
    {
        EnsureNote();
        var sourceLabel = Clean(SourceBox.Text);
        if (sourceLabel.Length == 0 || sourceLabel == "Reference...")
        {
            sourceLabel = "Unknown";
        }

        var locatorValue = Clean(LocatorBox.Text);
        var fragment = CaptureFragment.Create(
            text,
            new Source(sourceLabel),
            new Locator(locatorValue, locatorValue.Length == 0 ? LocatorKinds.None : LocatorKinds.Page),
            method);

        currentNote!.AddFragment(fragment);
        if (clearDraft)
        {
            DraftBox.Clear();
        }

        UpdatePreview();
        StatusText.Text = $"Captured fragment from {fragment.CitationHeading()}.";
    }

    private void ChangePage(int delta)
    {
        if (!int.TryParse(LocatorBox.Text.Trim(), out var page))
        {
            MessageBox.Show("Page must be a number.", "NoteMan");
            return;
        }

        LocatorBox.Text = Math.Max(1, page + delta).ToString();
    }

    private void UpdatePreview()
    {
        PreviewBox.Text = currentNote is null
            ? ""
            : FileProjectRepository.RenderNoteMarkdown(currentNote);
    }

    private bool EnsureWorkspace()
    {
        if (string.IsNullOrWhiteSpace(workspacePath) || !Directory.Exists(workspacePath))
        {
            MessageBox.Show("Choose a workspace before exporting.", "NoteMan");
            return false;
        }

        return true;
    }

    private void EnsureNote()
    {
        if (currentProject is null || currentNote is null)
        {
            NewNote_Click(this, new RoutedEventArgs());
        }
    }

    private static string Clean(string value) => value.Trim();

    private static string NormalizeCapturedText(string value) =>
        value.Replace("-\r\n", "", StringComparison.Ordinal)
            .Replace("-\n", "", StringComparison.Ordinal)
            .ReplaceLineEndings(" ")
            .Trim();

    private CaptureFragment? LatestFragment() =>
        currentNote is null || currentNote.Fragments.Count == 0
            ? null
            : currentNote.Fragments[^1];

    private void LoadPrompts()
    {
        prompts.Clear();
        var promptDirectory = Path.Combine(AppContext.BaseDirectory, "prompts");
        if (!Directory.Exists(promptDirectory))
        {
            prompts.Add(new PromptTemplate("Basic Research Note", "", DefaultPrompt()));
        }
        else
        {
            foreach (var path in Directory.GetFiles(promptDirectory, "*.txt").OrderBy(Path.GetFileName))
            {
                var content = File.ReadAllText(path).Trim();
                var title = content.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim()
                    ?? Path.GetFileNameWithoutExtension(path);
                prompts.Add(new PromptTemplate(title, path, content));
            }
        }

        PromptChoice.ItemsSource = prompts;
        PromptChoice.SelectedIndex = prompts.Count > 0 ? 0 : -1;
    }

    private PromptTemplate SelectedPrompt() =>
        PromptChoice.SelectedItem as PromptTemplate
        ?? prompts.FirstOrDefault()
        ?? new PromptTemplate("Basic Research Note", "", DefaultPrompt());

    private static string BuildPrompt(PromptTemplate prompt, CaptureFragment fragment) =>
        prompt.Body
            .Replace("{source}", fragment.Source.Label, StringComparison.Ordinal)
            .Replace("{locator}", fragment.Locator.Display(), StringComparison.Ordinal)
            .Replace("{fragment_text}", fragment.Text.Trim(), StringComparison.Ordinal);

    private static string DefaultPrompt() =>
        """
        Basic Research Note

        Source: {source}
        Locator: {locator}

        Captured text:
        {fragment_text}

        Task:
        Summarize this into a concise source-aware research note.
        """;

    private sealed record PromptTemplate(string Title, string Path, string Body);
}
