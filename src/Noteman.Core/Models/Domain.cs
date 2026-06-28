namespace Noteman.Core.Models;

public static class SourceTypes
{
    public const string Unknown = "unknown";
    public const string Book = "book";
    public const string Article = "article";
    public const string Pdf = "pdf";
    public const string Webpage = "webpage";
    public const string Lecture = "lecture";
    public const string Image = "image";
    public const string Clipboard = "clipboard";
}

public static class LocatorKinds
{
    public const string None = "none";
    public const string Page = "page";
    public const string PageRange = "page_range";
    public const string Timestamp = "timestamp";
    public const string Section = "section";
    public const string Url = "url";
    public const string File = "file";
}

public static class ExtractionMethods
{
    public const string Manual = "manual";
    public const string ClipboardText = "clipboard_text";
    public const string ClipboardOcr = "clipboard_ocr";
    public const string ImageOcr = "image_ocr";
    public const string PdfText = "pdf_text";
}

public sealed record Workspace(string Path);

public sealed record Project(
    string Name,
    string Id,
    string CreatedAt)
{
    public static Project Create(string name) =>
        new(name, Ids.New("project"), Clock.UtcNow());
}

public sealed record Source(
    string Label,
    string Type = SourceTypes.Unknown);

public sealed record Locator(
    string Value = "",
    string Kind = LocatorKinds.None)
{
    public string Display()
    {
        if (Kind == LocatorKinds.None || string.IsNullOrWhiteSpace(Value))
        {
            return "";
        }

        return Kind == LocatorKinds.Page
            ? $"p. {Value}"
            : $"{Kind}: {Value}";
    }
}

public sealed record Asset(
    string Path,
    string MediaType,
    string Id);

public sealed record CaptureFragment(
    string Text,
    Source Source,
    Locator Locator,
    string Method,
    string? AssetId,
    string Id,
    string CreatedAt)
{
    public static CaptureFragment Create(
        string text,
        Source source,
        Locator? locator = null,
        string method = ExtractionMethods.Manual,
        string? assetId = null)
    {
        if (string.IsNullOrWhiteSpace(text) && string.IsNullOrWhiteSpace(assetId))
        {
            throw new ArgumentException("A fragment needs text or a recoverable asset reference.");
        }

        return new CaptureFragment(
            text,
            source,
            locator ?? new Locator(),
            method,
            assetId,
            Ids.New("fragment"),
            Clock.UtcNow());
    }

    public string CitationHeading()
    {
        var locator = Locator.Display();
        return string.IsNullOrWhiteSpace(locator)
            ? Source.Label
            : $"{Source.Label}, {locator}";
    }
}

public sealed record Note(
    string Title,
    string Id,
    string CreatedAt,
    List<CaptureFragment> Fragments)
{
    public static Note Create(string title) =>
        new(title, Ids.New("note"), Clock.UtcNow(), []);

    public void AddFragment(CaptureFragment fragment) => Fragments.Add(fragment);
}

internal static class Ids
{
    public static string New(string prefix) =>
        $"{prefix}-{Guid.NewGuid():N}"[..(prefix.Length + 1 + 12)];
}

internal static class Clock
{
    public static string UtcNow() =>
        DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:sszzz");
}
