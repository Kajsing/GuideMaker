using GuideMaker.Core;
using GuideMaker.Export;
using GuideMaker.Storage;
using Xunit;

namespace GuideMaker.Tests;

public sealed class GuideExportWriterTests
{
    [Fact]
    public async Task ExportAsync_WritesMarkdownAndHtmlFiles()
    {
        var projectDirectory = Path.Combine(Path.GetTempPath(), "GuideMaker.Tests", Guid.NewGuid().ToString("N"));
        var document = GuideDocument.Create("Printer setup", "Codex") with
        {
            Steps =
            [
                GuideStep.Create(1, "Open settings", "Open the Windows Settings app.")
            ]
        };

        try
        {
            var writer = new GuideExportWriter();

            var result = await writer.ExportAsync(projectDirectory, document);

            Assert.True(File.Exists(result.MarkdownPath));
            Assert.True(File.Exists(result.HtmlPath));
            Assert.True(File.Exists(result.PdfPath));
            Assert.EndsWith(Path.Combine(GuideProjectLayout.ExportsDirectoryName, GuideExportWriter.MarkdownFileName), result.MarkdownPath);
            Assert.EndsWith(Path.Combine(GuideProjectLayout.ExportsDirectoryName, GuideExportWriter.HtmlFileName), result.HtmlPath);
            Assert.EndsWith(Path.Combine(GuideProjectLayout.ExportsDirectoryName, GuideExportWriter.PdfFileName), result.PdfPath);

            var markdown = await File.ReadAllTextAsync(result.MarkdownPath);
            var html = await File.ReadAllTextAsync(result.HtmlPath);
            var pdf = await File.ReadAllBytesAsync(result.PdfPath);

            Assert.Contains("# Printer setup", markdown);
            Assert.Contains("<h1>Printer setup</h1>", html);
            Assert.Equal("%PDF-"u8.ToArray(), pdf.Take(5).ToArray());
        }
        finally
        {
            if (Directory.Exists(projectDirectory))
            {
                Directory.Delete(projectDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ExportAsync_ExportsStarterGuideSample()
    {
        var sampleDirectory = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "samples",
            "starter-guide"));

        var sandboxDirectory = Path.Combine(Path.GetTempPath(), "GuideMaker.Tests", Guid.NewGuid().ToString("N"));

        try
        {
            CopyDirectory(sampleDirectory, sandboxDirectory);
            var project = await new GuideProjectStore().LoadAsync(sandboxDirectory);

            var result = await new GuideExportWriter().ExportAsync(sandboxDirectory, project.Document);

            var markdown = await File.ReadAllTextAsync(result.MarkdownPath);
            var html = await File.ReadAllTextAsync(result.HtmlPath);
            var pdf = await File.ReadAllBytesAsync(result.PdfPath);

            Assert.Contains("# Starter guide", markdown);
            Assert.Contains("![Screenshot of the application start screen.](assets/open-application.png)", markdown);
            Assert.Contains("<h1>Starter guide</h1>", html);
            Assert.Contains("<img src=\"assets/open-application.png\" alt=\"Screenshot of the application start screen.\">", html);
            Assert.Equal("%PDF-"u8.ToArray(), pdf.Take(5).ToArray());
        }
        finally
        {
            if (Directory.Exists(sandboxDirectory))
            {
                Directory.Delete(sandboxDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ExportAsync_WritesRenderedExportAssetsForAnnotatedImages()
    {
        var projectDirectory = Path.Combine(Path.GetTempPath(), "GuideMaker.Tests", Guid.NewGuid().ToString("N"));
        var assetId = Guid.NewGuid();
        var assetDirectory = Path.Combine(projectDirectory, GuideProjectLayout.AssetsDirectoryName);
        var sourceImagePath = Path.Combine(assetDirectory, "screen.png");
        var document = GuideDocument.Create("Annotated guide", "Codex") with
        {
            Steps =
            [
                GuideStep.Create(1, "Mark screen", "[[image:assets/screen.png]]") with
                {
                    AssetIds = [assetId],
                    Annotations =
                    [
                        new GuideAnnotation
                        {
                            Id = Guid.NewGuid(),
                            Kind = GuideAnnotationKind.Blur,
                            AssetId = assetId,
                            Bounds = new AnnotationBounds
                            {
                                X = 0,
                                Y = 0,
                                Width = 1,
                                Height = 1
                            }
                        }
                    ]
                }
            ],
            Assets =
            [
                new GuideAsset
                {
                    Id = assetId,
                    RelativePath = "assets/screen.png",
                    Kind = GuideAssetKind.Screenshot,
                    Caption = "Screen",
                    CapturedAt = DateTimeOffset.UtcNow
                }
            ]
        };

        try
        {
            Directory.CreateDirectory(assetDirectory);
            await File.WriteAllBytesAsync(sourceImagePath, Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII="));

            var result = await new GuideExportWriter().ExportAsync(projectDirectory, document);

            var renderedImagePath = Path.Combine(projectDirectory, "exports", "assets", "screen.png");
            var markdown = await File.ReadAllTextAsync(result.MarkdownPath);
            var html = await File.ReadAllTextAsync(result.HtmlPath);

            Assert.True(File.Exists(renderedImagePath));
            Assert.Contains("![Screen](assets/screen.png)", markdown);
            Assert.Contains("<img src=\"assets/screen.png\" alt=\"Screen\">", html);
            Assert.DoesNotContain("class=\"guide-annotation guide-annotation-blur\"", html);
            Assert.NotEqual(await File.ReadAllBytesAsync(sourceImagePath), await File.ReadAllBytesAsync(renderedImagePath));
        }
        finally
        {
            if (Directory.Exists(projectDirectory))
            {
                Directory.Delete(projectDirectory, recursive: true);
            }
        }
    }

    private static void CopyDirectory(string sourceDirectory, string destinationDirectory)
    {
        Directory.CreateDirectory(destinationDirectory);

        foreach (var directory in Directory.EnumerateDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(directory.Replace(sourceDirectory, destinationDirectory, StringComparison.Ordinal));
        }

        foreach (var file in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            File.Copy(
                file,
                file.Replace(sourceDirectory, destinationDirectory, StringComparison.Ordinal),
                overwrite: true);
        }
    }
}
