using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Noteman.Core.Models;
using Noteman.Core.Storage;

namespace Noteman.Desktop;

public partial class MainWindow : Window
{
    private const string ConfigFolderName = "noteman-desktop";
    private const string ConfigFileName = "desktop_app.json";

    private string? workspacePath;
    private string? lastWorkspacePath;
    private Project? currentProject;
    private Note? currentNote;
    private Dictionary<string, string> noteChoiceIndex = [];
    private bool draftLoadedFromNote;
    private readonly List<PromptTemplate> prompts = [];

    public MainWindow()
    {
        InitializeComponent();
        lastWorkspacePath = LoadLastWorkspace();
        LoadPrompts();
    }

    private void ChooseWorkspace_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Choose NoteMan workspace",
            InitialDirectory = WorkspaceDialogInitialDirectory(workspacePath, lastWorkspacePath)
        };

        if (dialog.ShowDialog() == true)
        {
            workspacePath = dialog.FolderName;
            lastWorkspacePath = workspacePath;
            WorkspacePathText.Text = workspacePath;
            RefreshProjectChoices();
            RefreshNoteChoices();
            try
            {
                SaveLastWorkspace(workspacePath);
            }
            catch (Exception ex)
                when (ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
            {
                StatusText.Text = "Workspace selected. Last workspace preference could not be saved.";
                return;
            }

            StatusText.Text = "Workspace selected.";
        }
    }

    private void NewNote_Click(object sender, RoutedEventArgs e)
    {
        var projectName = Clean(ProjectChoice.Text);
        var noteTitle = Clean(NoteChoice.Text);
        if (projectName.Length == 0 || noteTitle.Length == 0)
        {
            MessageBox.Show("Project and note title are required.", "NoteMan");
            return;
        }

        currentProject = LoadOrCreateProject(projectName);
        currentNote = Note.Create(noteTitle);
        DraftBox.Clear();
        draftLoadedFromNote = false;
        UpdatePreview();
        MessageBox.Show($"New note with {noteTitle} created.", "NoteMan");
        StatusText.Text = $"New note with {noteTitle} created.";
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

        if (!string.IsNullOrWhiteSpace(DraftBox.Text) && !DraftMatchesLoadedAiRetrieval())
        {
            AddFragment(DraftBox.Text, ExtractionMethods.Manual, clearDraft: true);
        }

        var repository = new FileProjectRepository(workspacePath!);
        var notePath = repository.SaveNote(currentProject!, currentNote!);
        RefreshProjectChoices();
        RefreshNoteChoices();
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
        draftLoadedFromNote = false;
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
        draftLoadedFromNote = false;
        StatusText.Text = "Pasted AI result into Typed / AI Draft. Review it before saving.";
    }

    private void SaveDraftAsFragment_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(DraftBox.Text))
        {
            StatusText.Text = "Typed / AI Draft is empty.";
            return;
        }
        if (DraftMatchesLoadedAiRetrieval())
        {
            StatusText.Text = "Loaded AI draft text is already saved.";
            return;
        }

        if (string.IsNullOrWhiteSpace(workspacePath) || !Directory.Exists(workspacePath))
        {
            MessageBox.Show("Choose a workspace before saving AI draft.", "NoteMan");
            return;
        }

        EnsureNote();
        var fragment = BuildFragment(DraftBox.Text, ExtractionMethods.AiDraft);
        var repository = new FileProjectRepository(workspacePath);
        var corpusPath = repository.SaveAiCorpusEntry(currentProject!, currentNote!, fragment);

        currentNote!.AddFragment(fragment);
        DraftBox.Clear();
        draftLoadedFromNote = false;
        UpdatePreview();
        RefreshProjectChoices();
        RefreshNoteChoices();
        StatusText.Text = $"Saved AI draft to {corpusPath}.";
    }

    private void AddFragment(string text, string method, bool clearDraft = false)
    {
        EnsureNote();
        var fragment = BuildFragment(text, method);
        currentNote!.AddFragment(fragment);
        if (clearDraft)
        {
            DraftBox.Clear();
            draftLoadedFromNote = false;
        }

        UpdatePreview();
        StatusText.Text = $"Captured fragment from {fragment.CitationHeading()}.";
    }

    private CaptureFragment BuildFragment(string text, string method)
    {
        var sourceLabel = Clean(SourceBox.Text);
        if (sourceLabel.Length == 0 || sourceLabel == "Reference...")
        {
            sourceLabel = "Unknown";
        }

        var locatorValue = Clean(LocatorBox.Text);
        return CaptureFragment.Create(
            text,
            new Source(sourceLabel),
            new Locator(locatorValue, locatorValue.Length == 0 ? LocatorKinds.None : LocatorKinds.Page),
            method);
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
            : RenderNoteByAiMethod(includeAi: false);
    }

    private string RenderNoteByAiMethod(bool includeAi)
    {
        if (currentNote is null)
        {
            return "";
        }

        var lines = new List<string> { $"# {currentNote.Title}", "" };
        foreach (var fragment in currentNote.Fragments)
        {
            var isAi = fragment.Method == ExtractionMethods.AiDraft;
            if (isAi == includeAi)
            {
                lines.AddRange(FileProjectRepository.RenderFragment(fragment));
            }
        }

        return string.Join("\n", lines).TrimEnd() + "\n";
    }

    private void RefreshProjectChoices()
    {
        if (string.IsNullOrWhiteSpace(workspacePath))
        {
            return;
        }

        var currentText = ProjectChoice.Text;
        ProjectChoice.ItemsSource = new FileProjectRepository(workspacePath).ListProjectNames();
        ProjectChoice.Text = currentText;
    }

    private void RefreshNoteChoices()
    {
        if (string.IsNullOrWhiteSpace(workspacePath))
        {
            return;
        }

        var currentText = NoteChoice.Text;
        var summaries = new FileProjectRepository(workspacePath).ListNoteSummaries(Clean(ProjectChoice.Text));
        var titleCounts = summaries
            .GroupBy(summary => summary.Title)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        noteChoiceIndex = [];
        var values = new List<string>();
        foreach (var summary in summaries)
        {
            var display = titleCounts[summary.Title] == 1
                ? summary.Title
                : $"{summary.Title} [{summary.Id}]";
            noteChoiceIndex[display] = summary.Id;
            values.Add(display);
        }

        NoteChoice.ItemsSource = values;
        NoteChoice.Text = currentText;
    }

    private void ProjectChoice_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProjectChoice.SelectedItem is string selectedProject)
        {
            ProjectChoice.Text = selectedProject;
        }

        RefreshNoteChoices();
    }

    private void ProjectChoice_LostFocus(object sender, RoutedEventArgs e) => RefreshNoteChoices();

    private void NoteChoice_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var display = NoteChoice.SelectedItem as string ?? Clean(NoteChoice.Text);
        LoadNoteForDisplay(display);
    }

    private bool LoadNoteForDisplay(string display)
    {
        if (string.IsNullOrWhiteSpace(workspacePath) || !noteChoiceIndex.TryGetValue(display, out var noteId))
        {
            return false;
        }

        var projectName = Clean(ProjectChoice.Text);
        var repository = new FileProjectRepository(workspacePath);
        var note = repository.LoadNote(projectName, noteId);
        if (note is null)
        {
            StatusText.Text = "Selected note could not be loaded.";
            return false;
        }

        currentProject = LoadOrCreateProject(projectName);
        currentNote = note;
        NoteChoice.Text = note.Title;
        var aiRetrieval = RenderNoteByAiMethod(includeAi: true);
        DraftBox.Text = aiRetrieval.Trim() == $"# {note.Title}" ? "" : aiRetrieval;
        draftLoadedFromNote = !string.IsNullOrWhiteSpace(DraftBox.Text);
        UpdatePreview();
        StatusText.Text = "Loaded existing note.";
        return true;
    }

    private bool DraftMatchesLoadedAiRetrieval()
    {
        return draftLoadedFromNote
            && currentNote is not null
            && string.Equals(DraftBox.Text.Trim(), RenderNoteByAiMethod(includeAi: true).Trim(), StringComparison.Ordinal);
    }

    private Project LoadOrCreateProject(string projectName)
    {
        if (string.IsNullOrWhiteSpace(workspacePath))
        {
            return Project.Create(projectName);
        }

        return new FileProjectRepository(workspacePath).LoadProject(projectName) ?? Project.Create(projectName);
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

    private static string DesktopConfigPath()
    {
        var configRoot = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (string.IsNullOrWhiteSpace(configRoot))
        {
            configRoot = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        return Path.Combine(configRoot, ConfigFolderName, ConfigFileName);
    }

    private static string? LoadLastWorkspace()
    {
        try
        {
            var configPath = DesktopConfigPath();
            if (!File.Exists(configPath))
            {
                return null;
            }

            var preferences = JsonSerializer.Deserialize<DesktopPreferences>(File.ReadAllText(configPath));
            return string.IsNullOrWhiteSpace(preferences?.LastWorkspace) ? null : preferences.LastWorkspace;
        }
        catch (Exception ex)
            when (ex is IOException or UnauthorizedAccessException or JsonException or NotSupportedException or ArgumentException)
        {
            return null;
        }
    }

    private static void SaveLastWorkspace(string selectedWorkspace)
    {
        var configPath = DesktopConfigPath();
        Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
        var preferences = new DesktopPreferences(Path.GetFullPath(selectedWorkspace));
        File.WriteAllText(
            configPath,
            JsonSerializer.Serialize(preferences, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static string WorkspaceDialogInitialDirectory(string? currentWorkspace, string? lastWorkspace)
    {
        if (!string.IsNullOrWhiteSpace(currentWorkspace) && Directory.Exists(currentWorkspace))
        {
            return currentWorkspace;
        }

        if (!string.IsNullOrWhiteSpace(lastWorkspace))
        {
            if (Directory.Exists(lastWorkspace))
            {
                return lastWorkspace;
            }

            var parent = SafeParentDirectory(lastWorkspace);
            if (parent is not null && parent.Exists)
            {
                return parent.FullName;
            }
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return string.IsNullOrWhiteSpace(home) ? Environment.CurrentDirectory : home;
    }

    private static DirectoryInfo? SafeParentDirectory(string path)
    {
        try
        {
            return Directory.GetParent(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
        {
            return null;
        }
    }

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

    private sealed record DesktopPreferences(string LastWorkspace);
}
