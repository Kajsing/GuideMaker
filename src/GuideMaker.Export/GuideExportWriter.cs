using GuideMaker.Core;

namespace GuideMaker.Export;

public sealed class GuideExportWriter
{
    public const string MarkdownFileName = "guide.md";
    public const string HtmlFileName = "guide.html";
    public const string PdfFileName = "guide.pdf";

    private readonly MarkdownGuideExporter markdownExporter;
    private readonly HtmlGuideExporter htmlExporter;
    private readonly PdfGuideExporter pdfExporter;
    private readonly ExportAssetRenderer assetRenderer;

    public GuideExportWriter()
        : this(new MarkdownGuideExporter(), new HtmlGuideExporter(), new PdfGuideExporter(), new ExportAssetRenderer())
    {
    }

    public GuideExportWriter(
        MarkdownGuideExporter markdownExporter,
        HtmlGuideExporter htmlExporter,
        PdfGuideExporter pdfExporter,
        ExportAssetRenderer assetRenderer)
    {
        this.markdownExporter = markdownExporter;
        this.htmlExporter = htmlExporter;
        this.pdfExporter = pdfExporter;
        this.assetRenderer = assetRenderer;
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
        var pdfPath = Path.Combine(exportsDirectory, PdfFileName);
        var exportDocument = assetRenderer.RenderExportAssets(projectDirectory, exportsDirectory, document);

        await File.WriteAllTextAsync(markdownPath, markdownExporter.Export(exportDocument), cancellationToken)
            .ConfigureAwait(false);
        await File.WriteAllTextAsync(htmlPath, htmlExporter.Export(exportDocument), cancellationToken)
            .ConfigureAwait(false);
        await File.WriteAllBytesAsync(pdfPath, pdfExporter.Export(exportDocument, exportsDirectory), cancellationToken)
            .ConfigureAwait(false);

        return new GuideExportResult
        {
            MarkdownPath = markdownPath,
            HtmlPath = htmlPath,
            PdfPath = pdfPath
        };
    }
}
