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
    private string selectedPromptGroup = "Research";

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

    private void RetrieveNote_Click(object sender, RoutedEventArgs e)
    {
        var display = NoteChoice.SelectedItem as string ?? Clean(NoteChoice.Text);
        if (!LoadNoteForDisplay(display))
        {
            StatusText.Text = "Select an exported note from the Note list to retrieve it.";
        }
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
            AddFragment(DraftBox.Text, ExtractionMethods.Manual);
        }

        var repository = new FileProjectRepository(workspacePath!);
        var notePath = repository.SaveNote(currentProject!, currentNote!);
        PreviewBox.Clear();
        DraftBox.Clear();
        draftLoadedFromNote = false;
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
        if (!EnsureWorkspace())
        {
            return;
        }

        var fragment = LatestFragment();
        if (fragment is null || currentProject is null || currentNote is null)
        {
            StatusText.Text = "Capture text first, then copy a prompt.";
            return;
        }

        var selectedPrompt = SelectedPrompt();
        var renderedPrompt = BuildPrompt(selectedPrompt, fragment);
        try
        {
            var repository = new FileProjectRepository(workspacePath!);
            repository.SavePromptUse(
                currentProject,
                currentNote,
                fragment,
                selectedPrompt.Title,
                selectedPrompt.Group == "User" ? "user" : "built_in",
                selectedPrompt.Body,
                renderedPrompt);
            PromptBox.Text = renderedPrompt;
            Clipboard.SetText(renderedPrompt);
            StatusText.Text = $"Copied and logged {selectedPrompt.Title} prompt for {fragment.CitationHeading()}.";
        }
        catch (Exception ex)
            when (ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
        {
            MessageBox.Show($"The prompt could not be logged and was not copied: {ex.Message}", "NoteMan");
        }
    }

    private void PasteAiResult_Click(object sender, RoutedEventArgs e)
    {
        if (!Clipboard.ContainsText())
        {
            StatusText.Text = "Clipboard has no AI result text.";
            return;
        }

        var pastedText = Clipboard.GetText().Trim();
        if (!string.IsNullOrWhiteSpace(DraftBox.Text))
        {
            DraftBox.AppendText($"{Environment.NewLine}{Environment.NewLine}");
        }

        DraftBox.AppendText(pastedText);
        DraftBox.CaretIndex = DraftBox.Text.Length;
        DraftBox.ScrollToEnd();
        DraftBox.Focus();
        draftLoadedFromNote = false;
        StatusText.Text = "Appended AI result to Typed / AI Draft. Review it before saving.";
    }

    private void AddPrompt_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new PromptEditorDialog { Owner = this };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        if (prompts.Any(prompt => string.Equals(prompt.Title, dialog.PromptTitle, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show("A prompt with that name already exists.", "NoteMan");
            return;
        }

        var content = $"{dialog.PromptTitle}{Environment.NewLine}Group: User{Environment.NewLine}{Environment.NewLine}{dialog.PromptBody}";
        PromptTemplate prompt;
        if (dialog.KeepAfterClosing)
        {
            try
            {
                var directory = UserPromptDirectory();
                Directory.CreateDirectory(directory);
                var path = UniquePromptPath(directory, dialog.PromptTitle);
                File.WriteAllText(path, content);
                prompt = ReadPrompt(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
            {
                MessageBox.Show($"The prompt could not be saved: {ex.Message}", "NoteMan");
                return;
            }
        }
        else
        {
            prompt = ReadPromptContent(content, "");
        }

        prompts.Add(prompt);
        RefreshPromptGroups("User");
        RefreshPromptChoices(prompt.Title);
        StatusText.Text = dialog.KeepAfterClosing
            ? $"Added user prompt '{prompt.Title}'."
            : $"Added temporary prompt '{prompt.Title}' for this session.";
    }

    private void RemovePrompt_Click(object sender, RoutedEventArgs e)
    {
        var prompt = SelectedPrompt();
        if (prompt.Group != "User")
        {
            StatusText.Text = "Only user-defined prompts can be removed here.";
            return;
        }

        var answer = MessageBox.Show($"Remove the user prompt '{prompt.Title}'?", "NoteMan",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (answer != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            if (prompt.Path.Length > 0 && File.Exists(prompt.Path))
            {
                File.Delete(prompt.Path);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
        {
            MessageBox.Show($"The prompt could not be removed: {ex.Message}", "NoteMan");
            return;
        }

        prompts.Remove(prompt);
        RefreshPromptGroups();
        RefreshPromptChoices();
        StatusText.Text = $"Removed user prompt '{prompt.Title}'.";
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
        var latestCapture = currentNote?.Fragments.LastOrDefault(
            fragment => fragment.Method != ExtractionMethods.AiDraft);
        PreviewBox.Text = latestCapture is null
            ? ""
            : string.Join("\n", FileProjectRepository.RenderFragment(latestCapture)).TrimEnd() + "\n";
        PreviewBox.CaretIndex = 0;
        PreviewBox.ScrollToHome();
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
        NoteChoice.Text = display;
        DraftBox.Text = FileProjectRepository.RenderNoteMarkdown(note);
        DraftBox.CaretIndex = 0;
        DraftBox.ScrollToHome();
        draftLoadedFromNote = true;
        UpdatePreview();
        StatusText.Text = "Retrieved existing note into Typed / AI Draft.";
        return true;
    }

    private bool DraftMatchesLoadedAiRetrieval()
    {
        return draftLoadedFromNote
            && currentNote is not null
            && string.Equals(
                DraftBox.Text.Trim(),
                FileProjectRepository.RenderNoteMarkdown(currentNote).Trim(),
                StringComparison.Ordinal);
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
            prompts.Add(new PromptTemplate("Basic Research Note", "", DefaultPrompt(), "Research"));
        }
        else
        {
            foreach (var path in Directory.GetFiles(promptDirectory, "*.txt").OrderBy(Path.GetFileName))
            {
                prompts.Add(ReadPrompt(path));
            }
        }

        var userPromptDirectory = UserPromptDirectory();
        if (Directory.Exists(userPromptDirectory))
        {
            foreach (var path in Directory.GetFiles(userPromptDirectory, "*.txt").OrderBy(Path.GetFileName))
            {
                prompts.Add(ReadPrompt(path));
            }
        }

        RefreshPromptGroups();
        RefreshPromptChoices();
    }

    private PromptTemplate SelectedPrompt() =>
        PromptChoice.SelectedItem as PromptTemplate
        ?? prompts.FirstOrDefault()
        ?? new PromptTemplate("Basic Research Note", "", DefaultPrompt(), "Research");

    private static PromptTemplate ReadPrompt(string path) => ReadPromptContent(File.ReadAllText(path), path);

    private static PromptTemplate ReadPromptContent(string content, string path)
    {
        content = content.Trim();
        var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var titleIndex = Array.FindIndex(lines, line => line.Trim().Length > 0);
        if (titleIndex < 0)
        {
            return new PromptTemplate(Path.GetFileNameWithoutExtension(path), path, content, "Research");
        }

        var title = lines[titleIndex].Trim();
        var group = "Research";
        var bodyLines = lines.ToList();
        for (var index = titleIndex + 1; index < lines.Length; index++)
        {
            var line = lines[index].Trim();
            if (line.Length == 0)
            {
                continue;
            }

            if (line.StartsWith("Group:", StringComparison.OrdinalIgnoreCase))
            {
                group = line.Substring("Group:".Length).Trim();
                if (group.Length == 0)
                {
                    group = "Research";
                }
                bodyLines.RemoveAt(index);
            }
            break;
        }

        return new PromptTemplate(title, path, string.Join(Environment.NewLine, bodyLines).Trim(), group);
    }

    private void RefreshPromptGroups(string? preferredGroup = null)
    {
        PromptGroupPanel.Children.Clear();
        var groups = prompts.Select(prompt => prompt.Group).Distinct().OrderBy(group => group).ToList();
        if (groups.Remove("Research"))
        {
            groups.Insert(0, "Research");
        }
        selectedPromptGroup = preferredGroup is not null && groups.Contains(preferredGroup)
            ? preferredGroup
            : groups.FirstOrDefault() ?? "Research";

        foreach (var group in groups)
        {
            var button = new RadioButton
            {
                Content = group,
                GroupName = "PromptGroups",
                IsChecked = group == selectedPromptGroup,
                Margin = new Thickness(0, 0, 12, 0)
            };
            button.Checked += PromptGroup_Checked;
            PromptGroupPanel.Children.Add(button);
        }
    }

    private void PromptGroup_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton button && button.Content is string group)
        {
            selectedPromptGroup = group;
            RefreshPromptChoices();
        }
    }

    private void RefreshPromptChoices(string? preferredTitle = null)
    {
        var groupedPrompts = prompts.Where(prompt => prompt.Group == selectedPromptGroup).ToList();
        PromptChoice.ItemsSource = groupedPrompts.Count > 0 ? groupedPrompts : prompts;
        PromptChoice.SelectedIndex = PromptChoice.Items.Count > 0 ? 0 : -1;
        if (preferredTitle is not null)
        {
            PromptChoice.SelectedItem = PromptChoice.Items.Cast<PromptTemplate>()
                .FirstOrDefault(prompt => prompt.Title == preferredTitle);
        }
    }

    private static string UserPromptDirectory() =>
        Path.Combine(Path.GetDirectoryName(DesktopConfigPath())!, "prompts");

    private static string UniquePromptPath(string directory, string title)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var stem = new string(title.Select(character => invalid.Contains(character) ? '-' : character).ToArray()).Trim();
        if (stem.Length == 0)
        {
            stem = "user-prompt";
        }

        var path = Path.Combine(directory, $"{stem}.txt");
        for (var suffix = 2; File.Exists(path); suffix++)
        {
            path = Path.Combine(directory, $"{stem}-{suffix}.txt");
        }
        return path;
    }

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

    private sealed record PromptTemplate(string Title, string Path, string Body, string Group);

    private sealed record DesktopPreferences(string LastWorkspace);
}
