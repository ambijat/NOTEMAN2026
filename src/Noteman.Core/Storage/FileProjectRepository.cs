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
        "",
        $"<!-- method: {fragment.Method}; fragment: {fragment.Id} -->",
        ""
    ];

    private void WriteJson<T>(string path, T value)
    {
        File.WriteAllText(path, JsonSerializer.Serialize(value, jsonOptions), Encoding.UTF8);
    }
}
