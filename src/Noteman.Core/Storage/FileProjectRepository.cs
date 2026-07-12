using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using Noteman.Core.Models;

namespace Noteman.Core.Storage;

public sealed record NoteSummary(string Id, string Title);

public sealed class FileProjectRepository
{
    private readonly string workspace;
    private readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true
    };
    private readonly JsonSerializerOptions jsonLinesOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false
    };

    public FileProjectRepository(string workspace)
    {
        this.workspace = workspace;
    }

    public string CreateProject(Project project)
    {
        var projectPath = Path.Combine(workspace, project.Name);
        Directory.CreateDirectory(projectPath);
        Directory.CreateDirectory(Path.Combine(projectPath, "assets"));
        Directory.CreateDirectory(Path.Combine(projectPath, "ai_corpus"));
        Directory.CreateDirectory(Path.Combine(projectPath, "notes"));
        Directory.CreateDirectory(Path.Combine(projectPath, "prompts", "snapshots"));
        WriteJson(Path.Combine(projectPath, "project.json"), project);
        return projectPath;
    }

    public string SavePromptUse(
        Project project,
        Note note,
        CaptureFragment fragment,
        string templateTitle,
        string templateOrigin,
        string templateText,
        string renderedPrompt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateTitle);
        ArgumentException.ThrowIfNullOrWhiteSpace(templateOrigin);
        ArgumentException.ThrowIfNullOrWhiteSpace(renderedPrompt);

        var projectPath = CreateProject(project);
        var promptsPath = Path.Combine(projectPath, "prompts");
        var snapshotsPath = Path.Combine(promptsPath, "snapshots");
        var usedAt = DateTimeOffset.UtcNow;
        var id = $"prompt-use-{usedAt:yyyyMMddTHHmmssfffZ}-{Guid.NewGuid():N}";
        var snapshotName = $"{id}.txt";
        var snapshotPath = Path.Combine(snapshotsPath, snapshotName);
        var relativeSnapshotPath = $"snapshots/{snapshotName}";

        File.WriteAllText(snapshotPath, renderedPrompt, new UTF8Encoding(false));

        var entry = new PromptUseEntry(
            id,
            usedAt.ToString("O"),
            templateTitle,
            templateOrigin,
            Sha256(templateText),
            Sha256(renderedPrompt),
            project.Id,
            project.Name,
            note.Id,
            note.Title,
            fragment.Id,
            fragment.Source.Label,
            fragment.Locator.Kind,
            fragment.Locator.Value,
            relativeSnapshotPath);

        var json = JsonSerializer.Serialize(entry, jsonLinesOptions);
        File.AppendAllText(Path.Combine(promptsPath, "usage.jsonl"), json + Environment.NewLine, new UTF8Encoding(false));
        return snapshotPath;
    }

    public string SaveNote(Project project, Note note)
    {
        var projectPath = CreateProject(project);
        var notesPath = Path.Combine(projectPath, "notes");
        var markdownPath = Path.Combine(notesPath, $"{note.Id}.md");
        var jsonPath = Path.Combine(notesPath, $"{note.Id}.json");

        File.WriteAllText(markdownPath, RenderNoteMarkdown(note), Encoding.UTF8);
        WriteJson(jsonPath, note);
        return markdownPath;
    }

    public IReadOnlyList<string> ListProjectNames()
    {
        if (!Directory.Exists(workspace))
        {
            return [];
        }

        return Directory.EnumerateDirectories(workspace)
            .Where(path => File.Exists(Path.Combine(path, "project.json")))
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public Project? LoadProject(string projectName)
    {
        var projectPath = Path.Combine(workspace, projectName, "project.json");
        try
        {
            return File.Exists(projectPath)
                ? JsonSerializer.Deserialize<Project>(File.ReadAllText(projectPath), jsonOptions)
                : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
        {
            return null;
        }
    }

    public IReadOnlyList<NoteSummary> ListNoteSummaries(string projectName)
    {
        var notesPath = Path.Combine(workspace, projectName, "notes");
        if (!Directory.Exists(notesPath))
        {
            return [];
        }

        var summaries = new List<NoteSummary>();
        foreach (var path in Directory.EnumerateFiles(notesPath, "*.json"))
        {
            try
            {
                var note = JsonSerializer.Deserialize<Note>(File.ReadAllText(path), jsonOptions);
                if (note is not null)
                {
                    summaries.Add(new NoteSummary(note.Id, note.Title));
                }
            }
            catch (Exception ex)
                when (ex is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
            {
                continue;
            }
        }

        return summaries
            .OrderBy(summary => summary.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(summary => summary.Id, StringComparer.Ordinal)
            .ToList();
    }

    public Note? LoadNote(string projectName, string noteId)
    {
        var notePath = Path.Combine(workspace, projectName, "notes", $"{noteId}.json");
        try
        {
            return File.Exists(notePath)
                ? JsonSerializer.Deserialize<Note>(File.ReadAllText(notePath), jsonOptions)
                : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
        {
            return null;
        }
    }

    public string SaveAiCorpusEntry(Project project, Note note, CaptureFragment fragment)
    {
        var projectPath = CreateProject(project);
        var corpusPath = Path.Combine(projectPath, "ai_corpus");
        var entryName = $"{note.Id}-{fragment.Id}";
        var markdownPath = Path.Combine(corpusPath, $"{entryName}.md");
        var jsonPath = Path.Combine(corpusPath, $"{entryName}.json");

        File.WriteAllText(markdownPath, RenderAiCorpusMarkdown(note, fragment), Encoding.UTF8);
        WriteJson(
            jsonPath,
            new AiCorpusEntry(
                entryName,
                note.Id,
                note.Title,
                fragment,
                fragment.CreatedAt));

        return markdownPath;
    }

    public static string RenderNoteMarkdown(Note note)
    {
        var lines = new List<string> { $"# {note.Title}", "" };
        foreach (var fragment in note.Fragments)
        {
            lines.AddRange(RenderFragment(fragment));
        }

        return string.Join("\n", lines).TrimEnd() + "\n";
    }

    public static IReadOnlyList<string> RenderFragment(CaptureFragment fragment) =>
    [
        $"## {fragment.CitationHeading()}",
        "",
        fragment.Text.Trim(),
        ""
    ];

    public static string RenderAiCorpusMarkdown(Note note, CaptureFragment fragment)
    {
        var lines = new List<string>
        {
            "# AI Draft Corpus Entry",
            "",
            $"Note: {note.Title}",
            $"Note ID: {note.Id}",
            $"Fragment ID: {fragment.Id}",
            $"Source: {fragment.CitationHeading()}",
            $"Method: {fragment.Method}",
            "",
            "## Draft Text",
            "",
            fragment.Text.Trim(),
            ""
        };

        return string.Join("\n", lines);
    }

    private void WriteJson<T>(string path, T value)
    {
        File.WriteAllText(path, JsonSerializer.Serialize(value, jsonOptions), Encoding.UTF8);
    }

    private static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private sealed record PromptUseEntry(
        string Id,
        string UsedAt,
        string TemplateTitle,
        string TemplateOrigin,
        string TemplateSha256,
        string RenderedSha256,
        string ProjectId,
        string ProjectName,
        string NoteId,
        string NoteTitle,
        string FragmentId,
        string Source,
        string LocatorKind,
        string LocatorValue,
        string Snapshot);

    private sealed record AiCorpusEntry(
        string Id,
        string NoteId,
        string NoteTitle,
        CaptureFragment Fragment,
        string CreatedAt);
}
