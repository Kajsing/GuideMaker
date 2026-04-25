namespace GuideMaker.Export;

public sealed record GuideExportResult
{
    public required string MarkdownPath { get; init; }

    public required string HtmlPath { get; init; }

    public required string PdfPath { get; init; }
}
