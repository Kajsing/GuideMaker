using GuideMaker.Core;

namespace GuideMaker.Export;

public sealed class GuideExportWriter
{
    public const string MarkdownFileName = "guide.md";
    public const string HtmlFileName = "guide.html";

    private readonly MarkdownGuideExporter markdownExporter;
    private readonly HtmlGuideExporter htmlExporter;

    public GuideExportWriter()
        : this(new MarkdownGuideExporter(), new HtmlGuideExporter())
    {
    }

    public GuideExportWriter(MarkdownGuideExporter markdownExporter, HtmlGuideExporter htmlExporter)
    {
        this.markdownExporter = markdownExporter;
        this.htmlExporter = htmlExporter;
    }

    public async Task<GuideExportResult> ExportAsync(
        string projectDirectory,
        GuideDocument document,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectDirectory);
        ArgumentNullException.ThrowIfNull(document);

        var exportsDirectory = Path.Combine(Path.GetFullPath(projectDirectory), GuideProjectLayout.ExportsDirectoryName);
        Directory.CreateDirectory(exportsDirectory);

        var markdownPath = Path.Combine(exportsDirectory, MarkdownFileName);
        var htmlPath = Path.Combine(exportsDirectory, HtmlFileName);

        await File.WriteAllTextAsync(markdownPath, markdownExporter.Export(document), cancellationToken)
            .ConfigureAwait(false);
        await File.WriteAllTextAsync(htmlPath, htmlExporter.Export(document), cancellationToken)
            .ConfigureAwait(false);

        return new GuideExportResult
        {
            MarkdownPath = markdownPath,
            HtmlPath = htmlPath
        };
    }
}
