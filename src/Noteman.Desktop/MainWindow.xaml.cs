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

    public MainWindow()
    {
        InitializeComponent();
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
            "OCR is the next adapter: this shell already preserves source, locator, fragment, and export state.",
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

    private void ResetDraft_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(DraftBox.Text))
        {
            var answer = MessageBox.Show(
                "Discard the current draft text?",
                "NoteMan",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (answer != MessageBoxResult.Yes)
            {
                return;
            }
        }

        DraftBox.Clear();
        StatusText.Text = "Draft reset.";
    }

    private void PreviousPage_Click(object sender, RoutedEventArgs e) => ChangePage(-1);

    private void NextPage_Click(object sender, RoutedEventArgs e) => ChangePage(1);

    private void Search_Click(object sender, RoutedEventArgs e)
    {
        SearchResults.Items.Clear();
        if (currentNote is null)
        {
            return;
        }

        var query = SearchBox.Text.Trim();
        if (query.Length == 0)
        {
            return;
        }

        foreach (var fragment in currentNote.Fragments)
        {
            if (fragment.Text.Contains(query, StringComparison.OrdinalIgnoreCase)
                || fragment.CitationHeading().Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                SearchResults.Items.Add($"{fragment.CitationHeading()} - {Preview(fragment.Text)}");
            }
        }
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

    private static string Preview(string value) =>
        value.Length <= 80 ? value : value[..80] + "...";
}
