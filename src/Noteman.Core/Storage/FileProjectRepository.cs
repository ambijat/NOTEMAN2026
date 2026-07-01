using System.Text;
using System.Text.Json;
using Noteman.Core.Models;

namespace Noteman.Core.Storage;

public sealed class FileProjectRepository
{
    private readonly string workspace;
    private readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true
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
        WriteJson(Path.Combine(projectPath, "project.json"), project);
        return projectPath;
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

    private sealed record AiCorpusEntry(
        string Id,
        string NoteId,
        string NoteTitle,
        CaptureFragment Fragment,
        string CreatedAt);
}
