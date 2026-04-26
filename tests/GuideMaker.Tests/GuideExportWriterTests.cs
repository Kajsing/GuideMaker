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

    [Fact]
    public async Task ExportAsync_WhenStepImageRefHasCropAndAnnotations_WritesRenderedStepImageAsset()
    {
        var projectDirectory = Path.Combine(Path.GetTempPath(), "GuideMaker.Tests", Guid.NewGuid().ToString("N"));
        var assetId = Guid.NewGuid();
        var imageRefId = Guid.NewGuid();
        var assetDirectory = Path.Combine(projectDirectory, GuideProjectLayout.AssetsDirectoryName);
        var sourceImagePath = Path.Combine(assetDirectory, "screen.png");
        var document = GuideDocument.Create("Cropped guide", "Codex") with
        {
            Steps =
            [
                GuideStep.Create(1, "Use cropped screen", "Before.\n[[image:assets/screen.png]]\nAfter.") with
                {
                    ImageRefs =
                    [
                        new StepImageRef
                        {
                            Id = imageRefId,
                            AssetId = assetId,
                            Crop = new ImageCropBounds
                            {
                                X = 0,
                                Y = 0,
                                Width = 0.5,
                                Height = 1
                            },
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
                    Caption = "Cropped screen",
                    CapturedAt = DateTimeOffset.UtcNow
                }
            ]
        };

        try
        {
            Directory.CreateDirectory(assetDirectory);
            await File.WriteAllBytesAsync(
                sourceImagePath,
                Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAQAAAACCAYAAAB/qH1jAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAOSURBVBhXY/iPBhjQBQAeIB/hYG4BMQAAAABJRU5ErkJggg=="));

            var result = await new GuideExportWriter().ExportAsync(projectDirectory, document);

            var renderedImagePath = Path.Combine(projectDirectory, "exports", "assets", "screen.png");
            var markdown = await File.ReadAllTextAsync(result.MarkdownPath);
            var html = await File.ReadAllTextAsync(result.HtmlPath);

            Assert.True(File.Exists(renderedImagePath));
            Assert.Contains("Before.", markdown);
            Assert.Contains("![Cropped screen](assets/screen.png)", markdown);
            Assert.Contains("After.", markdown);
            Assert.Contains("<img src=\"assets/screen.png\" alt=\"Cropped screen\">", html);
            Assert.DoesNotContain("class=\"guide-annotation guide-annotation-blur\"", html);
            Assert.Equal(1, CountOccurrences(markdown, "![Cropped screen](assets/screen.png)"));
            Assert.Equal(1, CountOccurrences(html, "<img src=\"assets/screen.png\""));

            var renderedDimensions = ReadPngDimensions(await File.ReadAllBytesAsync(renderedImagePath));
            Assert.Equal(2, renderedDimensions.Width);
            Assert.Equal(2, renderedDimensions.Height);
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
    public async Task ExportAsync_WhenStepImageRefHasCropWithoutAnnotations_WritesCroppedStepImageAsset()
    {
        var projectDirectory = Path.Combine(Path.GetTempPath(), "GuideMaker.Tests", Guid.NewGuid().ToString("N"));
        var assetId = Guid.NewGuid();
        var imageRefId = Guid.NewGuid();
        var assetDirectory = Path.Combine(projectDirectory, GuideProjectLayout.AssetsDirectoryName);
        var sourceImagePath = Path.Combine(assetDirectory, "screen.png");
        var document = GuideDocument.Create("Crop-only guide", "Codex") with
        {
            Steps =
            [
                GuideStep.Create(1, "Use cropped screen", "[[image:assets/screen.png]]") with
                {
                    ImageRefs =
                    [
                        new StepImageRef
                        {
                            Id = imageRefId,
                            AssetId = assetId,
                            Crop = new ImageCropBounds
                            {
                                X = 0.5,
                                Y = 0,
                                Width = 0.5,
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
                    Caption = "Crop-only screen",
                    CapturedAt = DateTimeOffset.UtcNow
                }
            ]
        };

        try
        {
            Directory.CreateDirectory(assetDirectory);
            await File.WriteAllBytesAsync(
                sourceImagePath,
                Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAQAAAACCAYAAAB/qH1jAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAOSURBVBhXY/iPBhjQBQAeIB/hYG4BMQAAAABJRU5ErkJggg=="));

            var result = await new GuideExportWriter().ExportAsync(projectDirectory, document);

            var renderedImagePath = Path.Combine(projectDirectory, "exports", "assets", "screen.png");
            var markdown = await File.ReadAllTextAsync(result.MarkdownPath);

            Assert.True(File.Exists(renderedImagePath));
            Assert.Contains("![Crop-only screen](assets/screen.png)", markdown);

            var renderedDimensions = ReadPngDimensions(await File.ReadAllBytesAsync(renderedImagePath));
            Assert.Equal(2, renderedDimensions.Width);
            Assert.Equal(2, renderedDimensions.Height);
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
    public async Task ExportAsync_WhenBodyUsesExplicitStepImageRefTokens_RendersSelectedImageRefs()
    {
        var projectDirectory = Path.Combine(Path.GetTempPath(), "GuideMaker.Tests", Guid.NewGuid().ToString("N"));
        var assetId = Guid.NewGuid();
        var firstImageRefId = Guid.NewGuid();
        var secondImageRefId = Guid.NewGuid();
        var assetDirectory = Path.Combine(projectDirectory, GuideProjectLayout.AssetsDirectoryName);
        var sourceImagePath = Path.Combine(assetDirectory, "screen.png");
        var document = GuideDocument.Create("Duplicate ref guide", "Codex") with
        {
            Steps =
            [
                GuideStep.Create(
                    1,
                    "Use same screen twice",
                    $"Second first.\n[[image-ref:{secondImageRefId}]]\nFirst second.\n[[image-ref:{firstImageRefId}]]") with
                {
                    ImageRefs =
                    [
                        new StepImageRef
                        {
                            Id = firstImageRefId,
                            AssetId = assetId,
                            Crop = new ImageCropBounds
                            {
                                X = 0,
                                Y = 0,
                                Width = 0.5,
                                Height = 1
                            }
                        },
                        new StepImageRef
                        {
                            Id = secondImageRefId,
                            AssetId = assetId,
                            Crop = new ImageCropBounds
                            {
                                X = 0.5,
                                Y = 0,
                                Width = 0.5,
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
                    Caption = "Duplicate screen",
                    CapturedAt = DateTimeOffset.UtcNow
                }
            ]
        };

        try
        {
            Directory.CreateDirectory(assetDirectory);
            await File.WriteAllBytesAsync(
                sourceImagePath,
                Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAQAAAACCAYAAAB/qH1jAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAOSURBVBhXY/iPBhjQBQAeIB/hYG4BMQAAAABJRU5ErkJggg=="));

            var result = await new GuideExportWriter().ExportAsync(projectDirectory, document);

            var markdown = await File.ReadAllTextAsync(result.MarkdownPath);
            var html = await File.ReadAllTextAsync(result.HtmlPath);

            Assert.Contains("Second first.", markdown);
            Assert.Contains("First second.", markdown);
            Assert.DoesNotContain("[[image-ref:", markdown);
            Assert.Contains("![Duplicate screen](assets/screen-2.png)", markdown);
            Assert.Contains("![Duplicate screen](assets/screen.png)", markdown);
            Assert.True(markdown.IndexOf("assets/screen-2.png", StringComparison.Ordinal) <
                markdown.IndexOf("assets/screen.png", StringComparison.Ordinal));
            Assert.Equal(1, CountOccurrences(html, "<img src=\"assets/screen-2.png\""));
            Assert.Equal(1, CountOccurrences(html, "<img src=\"assets/screen.png\""));
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
    public async Task ExportAsync_WhenStepImageRefHasArrowAnnotation_WritesRenderedStepImageAsset()
    {
        var projectDirectory = Path.Combine(Path.GetTempPath(), "GuideMaker.Tests", Guid.NewGuid().ToString("N"));
        var assetId = Guid.NewGuid();
        var imageRefId = Guid.NewGuid();
        var assetDirectory = Path.Combine(projectDirectory, GuideProjectLayout.AssetsDirectoryName);
        var sourceImagePath = Path.Combine(assetDirectory, "screen.png");
        var document = GuideDocument.Create("Arrow guide", "Codex") with
        {
            Steps =
            [
                GuideStep.Create(1, "Point at screen", "[[image:assets/screen.png]]") with
                {
                    ImageRefs =
                    [
                        new StepImageRef
                        {
                            Id = imageRefId,
                            AssetId = assetId,
                            Annotations =
                            [
                                new GuideAnnotation
                                {
                                    Id = Guid.NewGuid(),
                                    Kind = GuideAnnotationKind.Arrow,
                                    AssetId = assetId,
                                    Bounds = new AnnotationBounds
                                    {
                                        X = 0.1,
                                        Y = 0.25,
                                        Width = 0.8,
                                        Height = 0.5
                                    }
                                }
                            ]
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
                    Caption = "Arrow screen",
                    CapturedAt = DateTimeOffset.UtcNow
                }
            ]
        };

        try
        {
            Directory.CreateDirectory(assetDirectory);
            await File.WriteAllBytesAsync(
                sourceImagePath,
                Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAQAAAACCAYAAAB/qH1jAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAOSURBVBhXY/iPBhjQBQAeIB/hYG4BMQAAAABJRU5ErkJggg=="));

            var result = await new GuideExportWriter().ExportAsync(projectDirectory, document);

            var renderedImagePath = Path.Combine(projectDirectory, "exports", "assets", "screen.png");
            var markdown = await File.ReadAllTextAsync(result.MarkdownPath);
            var html = await File.ReadAllTextAsync(result.HtmlPath);

            Assert.True(File.Exists(renderedImagePath));
            Assert.Contains("![Arrow screen](assets/screen.png)", markdown);
            Assert.Contains("<img src=\"assets/screen.png\" alt=\"Arrow screen\">", html);
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

    [Fact]
    public async Task ExportAsync_WhenStepImageRefIsNotReferencedInBody_DoesNotRenderImageInGuide()
    {
        var projectDirectory = Path.Combine(Path.GetTempPath(), "GuideMaker.Tests", Guid.NewGuid().ToString("N"));
        var assetId = Guid.NewGuid();
        var assetDirectory = Path.Combine(projectDirectory, GuideProjectLayout.AssetsDirectoryName);
        var sourceImagePath = Path.Combine(assetDirectory, "screen.png");
        var document = GuideDocument.Create("Pool guide", "Codex") with
        {
            Steps =
            [
                GuideStep.Create(1, "Describe only", "No image token here.") with
                {
                    ImageRefs =
                    [
                        StepImageRef.Create(assetId)
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
                    Caption = "Pool image",
                    CapturedAt = DateTimeOffset.UtcNow
                }
            ]
        };

        try
        {
            Directory.CreateDirectory(assetDirectory);
            await File.WriteAllBytesAsync(
                sourceImagePath,
                Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAQAAAACCAYAAAB/qH1jAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAOSURBVBhXY/iPBhjQBQAeIB/hYG4BMQAAAABJRU5ErkJggg=="));

            var result = await new GuideExportWriter().ExportAsync(projectDirectory, document);

            var markdown = await File.ReadAllTextAsync(result.MarkdownPath);
            var html = await File.ReadAllTextAsync(result.HtmlPath);

            Assert.Contains("No image token here.", markdown);
            Assert.Contains("<p>No image token here.</p>", html);
            Assert.DoesNotContain("![Pool image]", markdown);
            Assert.DoesNotContain("<img src=\"assets/screen.png\"", html);
        }
        finally
        {
            if (Directory.Exists(projectDirectory))
            {
                Directory.Delete(projectDirectory, recursive: true);
            }
        }
    }

    private static int CountOccurrences(string value, string search)
    {
        var count = 0;
        var index = 0;

        while ((index = value.IndexOf(search, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += search.Length;
        }

        return count;
    }

    private static (int Width, int Height) ReadPngDimensions(byte[] pngBytes)
    {
        return (ReadBigEndianInt32(pngBytes, 16), ReadBigEndianInt32(pngBytes, 20));
    }

    private static int ReadBigEndianInt32(byte[] bytes, int offset)
    {
        return bytes[offset] << 24 |
            bytes[offset + 1] << 16 |
            bytes[offset + 2] << 8 |
            bytes[offset + 3];
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
